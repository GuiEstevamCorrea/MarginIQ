using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case: Trigger Incremental Learning (IA-UC-04 – Aprendizado Incremental)
/// Triggers periodic training of the AI model based on:
/// - Human decisions (manager approvals/rejections)
/// - Actual sale outcomes (won/lost)
/// Enables the AI to learn from real-world results and improve recommendations over time.
/// </summary>
public class TriggerIncrementalLearningUseCase
{
    private readonly IAILearningDataRepository _learningDataRepository;
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IAIService _aiService;

    public TriggerIncrementalLearningUseCase(
        IAILearningDataRepository learningDataRepository,
        IDiscountRequestRepository discountRequestRepository,
        IApprovalRepository approvalRepository,
        IAIService aiService)
    {
        _learningDataRepository = learningDataRepository;
        _discountRequestRepository = discountRequestRepository;
        _approvalRepository = approvalRepository;
        _aiService = aiService;
    }

    /// <summary>
    /// Executes incremental learning process
    /// </summary>
    public async Task<TriggerIncrementalLearningResponse> ExecuteAsync(
        TriggerIncrementalLearningRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var response = new TriggerIncrementalLearningResponse
        {
            Mode = request.Mode,
            UsedAI = request.UseAI
        };

        try
        {
            // Step 1: Collect training data
            var learningData = await CollectTrainingDataAsync(request, cancellationToken);

            response.DataPointsCollected = learningData.Count;

            // Step 2: Validate minimum data points
            if (learningData.Count < request.MinimumDataPoints && !request.ForceTraining)
            {
                response.Success = false;
                response.Message = $"Insufficient training data. Collected {learningData.Count} points, " +
                                 $"minimum required is {request.MinimumDataPoints}. Use ForceTraining=true to override.";
                response.Duration = DateTime.UtcNow - startTime;
                return response;
            }

            if (learningData.Count == 0)
            {
                response.Success = false;
                response.Message = "No training data available for the specified criteria.";
                response.Duration = DateTime.UtcNow - startTime;
                return response;
            }

            // Step 3: Filter and prepare training data
            var filteredData = FilterTrainingData(learningData, request);
            response.DataPointsProcessed = filteredData.Count;

            // Step 4: Calculate breakdown
            response.Breakdown = CalculateBreakdown(filteredData);

            // Step 5: Add warnings if needed
            AddWarnings(response, request, filteredData);

            // Step 6: Trigger AI training (if enabled)
            if (request.UseAI && await _aiService.IsAvailableAsync(request.CompanyId, cancellationToken))
            {
                var trainingResult = await TrainAIModelAsync(
                    request.CompanyId,
                    filteredData,
                    request.Mode,
                    cancellationToken);

                response.Success = trainingResult.Success;
                response.Message = trainingResult.Message;
                response.ModelVersion = trainingResult.ModelVersion;
                response.Metrics = ExtractMetrics(trainingResult);
            }
            else
            {
                // Simulation mode - just log the data
                response.Success = true;
                response.Message = request.UseAI
                    ? "Training data collected successfully. AI service not available - data logged for future training."
                    : "Training data collected successfully (simulation mode - no AI training performed).";
            }

            response.TrainedAt = DateTime.UtcNow;
            response.Duration = DateTime.UtcNow - startTime;

            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Training failed: {ex.Message}";
            response.Duration = DateTime.UtcNow - startTime;
            response.Warnings.Add($"Exception: {ex.GetType().Name}");
            return response;
        }
    }

    /// <summary>
    /// Collects training data from learning data repository
    /// </summary>
    private async Task<List<AILearningData>> CollectTrainingDataAsync(
        TriggerIncrementalLearningRequest request,
        CancellationToken cancellationToken)
    {
        IEnumerable<AILearningData> data;

        // Get data based on date range
        if (request.FromDate.HasValue || request.ToDate.HasValue)
        {
            var from = request.FromDate ?? DateTime.MinValue;
            var to = request.ToDate ?? DateTime.UtcNow;

            data = await _learningDataRepository.GetByDateRangeAsync(
                request.CompanyId,
                from,
                to);
        }
        else if (request.Mode == TrainingMode.Full)
        {
            // Full training - get all historical data
            data = await _learningDataRepository.GetByCompanyIdAsync(
                request.CompanyId,
                skip: 0,
                take: 10000); // Large number to get all data
        }
        else
        {
            // Incremental - get only untrained data that's ready
            data = await _learningDataRepository.GetReadyForTrainingAsync(
                request.CompanyId,
                maxAgeDays: null); // All untrained data
        }

        return data.ToList();
    }

    /// <summary>
    /// Filters training data based on request criteria
    /// </summary>
    private List<AILearningData> FilterTrainingData(
        List<AILearningData> data,
        TriggerIncrementalLearningRequest request)
    {
        var filtered = data.AsQueryable();

        // Filter by completed sales only
        if (request.OnlyCompletedSales)
        {
            filtered = filtered.Where(d => d.SaleOutcome.HasValue);
        }

        // Filter by decision source
        var allowedSources = new List<ApprovalSource>();
        if (request.IncludeHumanDecisions)
        {
            allowedSources.Add(ApprovalSource.Human);
        }
        if (request.IncludeAIDecisions)
        {
            allowedSources.Add(ApprovalSource.AI);
        }

        if (allowedSources.Any())
        {
            filtered = filtered.Where(d => allowedSources.Contains(d.DecisionSource));
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Calculates training breakdown statistics
    /// </summary>
    private TrainingBreakdown CalculateBreakdown(List<AILearningData> data)
    {
        return new TrainingBreakdown
        {
            HumanDecisions = data.Count(d => d.DecisionSource == ApprovalSource.Human),
            AIDecisions = data.Count(d => d.DecisionSource == ApprovalSource.AI),
            WithSaleOutcome = data.Count(d => d.SaleOutcome.HasValue),
            Approved = data.Count(d => d.Decision == ApprovalDecision.Approve),
            Rejected = data.Count(d => d.Decision == ApprovalDecision.Reject),
            SalesWon = data.Count(d => d.SaleOutcome == true),
            SalesLost = data.Count(d => d.SaleOutcome == false)
        };
    }

    /// <summary>
    /// Adds warnings based on training data quality
    /// </summary>
    private void AddWarnings(
        TriggerIncrementalLearningResponse response,
        TriggerIncrementalLearningRequest request,
        List<AILearningData> data)
    {
        // Warning: Low data volume
        if (data.Count < 50)
        {
            response.Warnings.Add($"Low training data volume ({data.Count} points). Model accuracy may be limited.");
        }

        // Warning: No sale outcomes
        var withOutcomes = data.Count(d => d.SaleOutcome.HasValue);
        if (withOutcomes == 0 && request.OnlyCompletedSales)
        {
            response.Warnings.Add("No completed sale outcomes found. Consider including pending decisions.");
        }

        // Warning: Imbalanced data
        var approvedCount = data.Count(d => d.Decision == ApprovalDecision.Approve);
        var rejectedCount = data.Count(d => d.Decision == ApprovalDecision.Reject);

        if (approvedCount > 0 && rejectedCount > 0)
        {
            var ratio = (decimal)Math.Max(approvedCount, rejectedCount) /
                       Math.Min(approvedCount, rejectedCount);

            if (ratio > 10)
            {
                response.Warnings.Add($"Highly imbalanced data: {approvedCount} approved vs {rejectedCount} rejected. " +
                                    "Model may be biased.");
            }
        }

        // Warning: Only AI decisions
        if (request.IncludeAIDecisions && !request.IncludeHumanDecisions)
        {
            response.Warnings.Add("Training only on AI decisions may reinforce existing biases. " +
                                "Consider including human decisions for better learning.");
        }

        // Warning: No recent data
        if (data.Any())
        {
            var mostRecentDate = data.Max(d => d.RequestCreatedAt);
            var daysSinceLastData = (DateTime.UtcNow - mostRecentDate).TotalDays;

            if (daysSinceLastData > 30)
            {
                response.Warnings.Add($"Most recent training data is {daysSinceLastData:F0} days old. " +
                                    "Model may not reflect current patterns.");
            }
        }
    }

    /// <summary>
    /// Triggers AI model training
    /// </summary>
    private async Task<TrainingResult> TrainAIModelAsync(
        Guid companyId,
        List<AILearningData> data,
        TrainingMode mode,
        CancellationToken cancellationToken)
    {
        // Convert learning data to training data points
        var trainingDataPoints = data.Select(d => new TrainingDataPoint
        {
            DiscountRequestId = d.DiscountRequestId,
            RequestedDiscount = d.RequestedDiscountPercentage,
            FinalMargin = d.FinalMarginPercentage,
            Decision = d.Decision.ToString(),
            DecisionSource = d.DecisionSource.ToString(),
            SaleOutcome = d.SaleOutcome, // Already nullable bool
            DecisionDate = d.DecisionMadeAt
        }).ToList();

        var trainingRequest = new ModelTrainingRequest
        {
            CompanyId = companyId,
            TrainingData = trainingDataPoints,
            Type = mode == TrainingMode.Incremental
                ? Application.Ports.TrainingType.Incremental
                : Application.Ports.TrainingType.Full
        };

        var result = await _aiService.TrainModelAsync(trainingRequest, cancellationToken);

        // Mark data as trained if successful
        if (result.Success)
        {
            await MarkDataAsTrainedAsync(data, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Marks learning data as trained
    /// </summary>
    private async Task MarkDataAsTrainedAsync(
        List<AILearningData> data,
        CancellationToken cancellationToken)
    {
        foreach (var item in data)
        {
            item.MarkAsUsedForTraining();
            await _learningDataRepository.UpdateAsync(item);
        }
    }

    /// <summary>
    /// Extracts training metrics from AI result
    /// </summary>
    private TrainingMetrics? ExtractMetrics(TrainingResult result)
    {
        // This would depend on what the AI service returns
        // For now, return null if no metrics available
        if (string.IsNullOrEmpty(result.ModelVersion))
        {
            return null;
        }

        return new TrainingMetrics
        {
            Accuracy = null, // Would come from AI service
            Confidence = null,
            Improvement = null,
            ErrorRate = null,
            AdditionalMetrics = new Dictionary<string, decimal>
            {
                { "DataPointsProcessed", result.DataPointsProcessed }
            }
        };
    }
}

/// <summary>
/// Extension methods for incremental learning
/// </summary>
public static class IncrementalLearningExtensions
{
    /// <summary>
    /// Creates a quick incremental learning request with defaults
    /// </summary>
    public static TriggerIncrementalLearningRequest CreateIncrementalRequest(Guid companyId)
    {
        return new TriggerIncrementalLearningRequest
        {
            CompanyId = companyId,
            Mode = TrainingMode.Incremental,
            OnlyCompletedSales = true,
            IncludeHumanDecisions = true,
            IncludeAIDecisions = false,
            MinimumDataPoints = 10,
            UseAI = true
        };
    }

    /// <summary>
    /// Creates a full retraining request
    /// </summary>
    public static TriggerIncrementalLearningRequest CreateFullRetrainingRequest(Guid companyId)
    {
        return new TriggerIncrementalLearningRequest
        {
            CompanyId = companyId,
            Mode = TrainingMode.Full,
            OnlyCompletedSales = true,
            IncludeHumanDecisions = true,
            IncludeAIDecisions = true,
            MinimumDataPoints = 50,
            UseAI = true
        };
    }
}
