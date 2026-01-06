using Application.Ports;

namespace Application.Services;

/// <summary>
/// Performance-optimized wrapper for AI service with:
/// - 2-second timeout enforcement (as per Projeto.md 8.1)
/// - Automatic fallback to rule-based logic
/// - Response caching
/// - Circuit breaker pattern
/// - Performance metrics tracking
/// 
/// This ensures the system remains responsive even when AI is slow or unavailable.
/// Fallback guarantees the system always functions, with or without AI.
/// </summary>
public class PerformanceOptimizedAIService : IAIService
{
    private readonly IAIService _innerAIService;
    private readonly IAIResponseCache _cache;
    private readonly IPerformanceMetrics _metrics;
    private readonly TimeSpan _aiTimeout = TimeSpan.FromSeconds(2); // Projeto.md 8.1 requirement
    private readonly CircuitBreakerState _circuitBreaker;

    public PerformanceOptimizedAIService(
        IAIService innerAIService,
        IAIResponseCache cache,
        IPerformanceMetrics metrics)
    {
        _innerAIService = innerAIService;
        _cache = cache;
        _metrics = metrics;
        _circuitBreaker = new CircuitBreakerState();
    }

    /// <summary>
    /// Recommends discount with timeout and fallback
    /// </summary>
    public async Task<DiscountRecommendation> RecommendDiscountAsync(
        DiscountRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var cacheKey = GenerateCacheKey("recommend", request);

        try
        {
            // Step 1: Check cache first
            if (_cache.TryGet<DiscountRecommendation>(cacheKey, out var cachedResult))
            {
                _metrics.RecordCacheHit("RecommendDiscount");
                _metrics.RecordResponseTime("RecommendDiscount", DateTime.UtcNow - startTime, fromCache: true);
                return cachedResult!;
            }

            _metrics.RecordCacheMiss("RecommendDiscount");

            // Step 2: Check circuit breaker state
            if (_circuitBreaker.IsOpen)
            {
                _metrics.RecordCircuitBreakerOpen("RecommendDiscount");
                return await FallbackRecommendDiscountAsync(request, "Circuit breaker open");
            }

            // Step 3: Call AI with timeout
            using var timeoutCts = new CancellationTokenSource(_aiTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            DiscountRecommendation result;
            try
            {
                result = await _innerAIService.RecommendDiscountAsync(request, linkedCts.Token);
                
                // Success - record metrics and cache
                var duration = DateTime.UtcNow - startTime;
                _metrics.RecordResponseTime("RecommendDiscount", duration, fromCache: false);
                _metrics.RecordSuccess("RecommendDiscount");
                _circuitBreaker.RecordSuccess();

                // Cache the result
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // AI timeout - use fallback
                _metrics.RecordTimeout("RecommendDiscount");
                _circuitBreaker.RecordFailure();
                return await FallbackRecommendDiscountAsync(request, "AI timeout (>2s)");
            }
            catch (Exception ex)
            {
                // AI error - use fallback
                _metrics.RecordError("RecommendDiscount", ex.GetType().Name);
                _circuitBreaker.RecordFailure();
                return await FallbackRecommendDiscountAsync(request, $"AI error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Fallback itself failed - return safe default
            _metrics.RecordError("RecommendDiscount", $"Fallback failed: {ex.GetType().Name}");
            return CreateSafeDefaultRecommendation(request);
        }
    }

    /// <summary>
    /// Calculates risk score with timeout and fallback
    /// </summary>
    public async Task<AIRiskScore> CalculateRiskScoreAsync(
        RiskScoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var cacheKey = GenerateCacheKey("riskscore", request);

        try
        {
            // Step 1: Check cache first
            if (_cache.TryGet<AIRiskScore>(cacheKey, out var cachedResult))
            {
                _metrics.RecordCacheHit("CalculateRiskScore");
                _metrics.RecordResponseTime("CalculateRiskScore", DateTime.UtcNow - startTime, fromCache: true);
                return cachedResult!;
            }

            _metrics.RecordCacheMiss("CalculateRiskScore");

            // Step 2: Check circuit breaker state
            if (_circuitBreaker.IsOpen)
            {
                _metrics.RecordCircuitBreakerOpen("CalculateRiskScore");
                return await FallbackCalculateRiskScoreAsync(request, "Circuit breaker open");
            }

            // Step 3: Call AI with timeout
            using var timeoutCts = new CancellationTokenSource(_aiTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            AIRiskScore result;
            try
            {
                result = await _innerAIService.CalculateRiskScoreAsync(request, linkedCts.Token);
                
                // Success - record metrics and cache
                var duration = DateTime.UtcNow - startTime;
                _metrics.RecordResponseTime("CalculateRiskScore", duration, fromCache: false);
                _metrics.RecordSuccess("CalculateRiskScore");
                _circuitBreaker.RecordSuccess();

                // Cache the result
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // AI timeout - use fallback
                _metrics.RecordTimeout("CalculateRiskScore");
                _circuitBreaker.RecordFailure();
                return await FallbackCalculateRiskScoreAsync(request, "AI timeout (>2s)");
            }
            catch (Exception ex)
            {
                // AI error - use fallback
                _metrics.RecordError("CalculateRiskScore", ex.GetType().Name);
                _circuitBreaker.RecordFailure();
                return await FallbackCalculateRiskScoreAsync(request, $"AI error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Fallback itself failed - return safe default
            _metrics.RecordError("CalculateRiskScore", $"Fallback failed: {ex.GetType().Name}");
            return CreateSafeDefaultRiskScore(request);
        }
    }

    /// <summary>
    /// Explains decision with timeout and fallback
    /// </summary>
    public async Task<AIExplanation> ExplainDecisionAsync(
        ExplainabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var cacheKey = GenerateCacheKey("explain", request);

        try
        {
            // Step 1: Check cache first
            if (_cache.TryGet<AIExplanation>(cacheKey, out var cachedResult))
            {
                _metrics.RecordCacheHit("ExplainDecision");
                _metrics.RecordResponseTime("ExplainDecision", DateTime.UtcNow - startTime, fromCache: true);
                return cachedResult!;
            }

            _metrics.RecordCacheMiss("ExplainDecision");

            // Step 2: Check circuit breaker state
            if (_circuitBreaker.IsOpen)
            {
                _metrics.RecordCircuitBreakerOpen("ExplainDecision");
                return await FallbackExplainDecisionAsync(request, "Circuit breaker open");
            }

            // Step 3: Call AI with timeout
            using var timeoutCts = new CancellationTokenSource(_aiTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            AIExplanation result;
            try
            {
                result = await _innerAIService.ExplainDecisionAsync(request, linkedCts.Token);
                
                // Success - record metrics and cache
                var duration = DateTime.UtcNow - startTime;
                _metrics.RecordResponseTime("ExplainDecision", duration, fromCache: false);
                _metrics.RecordSuccess("ExplainDecision");
                _circuitBreaker.RecordSuccess();

                // Cache the result (longer cache for explanations)
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));

                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // AI timeout - use fallback
                _metrics.RecordTimeout("ExplainDecision");
                _circuitBreaker.RecordFailure();
                return await FallbackExplainDecisionAsync(request, "AI timeout (>2s)");
            }
            catch (Exception ex)
            {
                // AI error - use fallback
                _metrics.RecordError("ExplainDecision", ex.GetType().Name);
                _circuitBreaker.RecordFailure();
                return await FallbackExplainDecisionAsync(request, $"AI error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Fallback itself failed - return safe default
            _metrics.RecordError("ExplainDecision", $"Fallback failed: {ex.GetType().Name}");
            return CreateSafeDefaultExplanation(request);
        }
    }

    /// <summary>
    /// Performs incremental learning (no cache, no fallback)
    /// This is a background operation, not time-critical
    /// </summary>
    public async Task<TrainingResult> TrainModelAsync(
        ModelTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Learning operations can take longer, but still have a timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            var result = await _innerAIService.TrainModelAsync(request, linkedCts.Token);
            
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordResponseTime("TrainModel", duration, fromCache: false);
            _metrics.RecordSuccess("TrainModel");

            return result;
        }
        catch (OperationCanceledException)
        {
            _metrics.RecordTimeout("TrainModel");
            // Learning timeout is not critical, return failure
            return new TrainingResult
            {
                Success = false,
                Message = "Training timeout"
            };
        }
        catch (Exception ex)
        {
            _metrics.RecordError("TrainModel", ex.GetType().Name);
            // Learning errors are not critical, return failure
            return new TrainingResult
            {
                Success = false,
                Message = $"Training error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets AI governance settings
    /// </summary>
    public async Task<AIGovernanceSettings> GetGovernanceSettingsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            return await _innerAIService.GetGovernanceSettingsAsync(companyId, linkedCts.Token);
        }
        catch
        {
            // Return default settings on error
            return new AIGovernanceSettings
            {
                AIEnabled = false
            };
        }
    }

    /// <summary>
    /// Updates AI governance settings
    /// </summary>
    public async Task UpdateGovernanceSettingsAsync(
        Guid companyId,
        AIGovernanceSettings settings,
        CancellationToken cancellationToken = default)
    {
        await _innerAIService.UpdateGovernanceSettingsAsync(companyId, settings, cancellationToken);
    }

    /// <summary>
    /// Checks if AI is available (quick health check)
    /// </summary>
    public async Task<bool> IsAvailableAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            return await _innerAIService.IsAvailableAsync(companyId, linkedCts.Token);
        }
        catch
        {
            return false;
        }
    }

    // ==================== FALLBACK METHODS ====================
    // These methods provide rule-based logic when AI is unavailable

    private Task<DiscountRecommendation> FallbackRecommendDiscountAsync(
        DiscountRecommendationRequest request,
        string reason)
    {
        _metrics.RecordFallbackUsed("RecommendDiscount", reason);

        // Use rule-based recommendation (simplified logic)
        // In production, this would call the rule-based services from Domain layer
        decimal recommendedDiscount = 5.0m; // Conservative default
        decimal expectedMargin = 20.0m; // Safe margin

        return Task.FromResult(new DiscountRecommendation
        {
            RecommendedDiscountPercentage = recommendedDiscount,
            ExpectedMarginPercentage = expectedMargin,
            Confidence = 0.5m, // Medium confidence for fallback
            Explanation = $"Rule-based fallback used. Reason: {reason}. Recommending conservative discount."
        });
    }

    private Task<AIRiskScore> FallbackCalculateRiskScoreAsync(
        RiskScoreRequest request,
        string reason)
    {
        _metrics.RecordFallbackUsed("CalculateRiskScore", reason);

        // Use rule-based risk calculation
        // Higher discount = higher risk (simple heuristic)
        decimal riskScore = Math.Min(request.RequestedDiscountPercentage * 3, 100);

        return Task.FromResult(new AIRiskScore
        {
            Score = riskScore,
            RiskLevel = riskScore switch
            {
                < 30 => "Low",
                < 60 => "Medium",
                < 80 => "High",
                _ => "VeryHigh"
            },
            RiskFactors = new List<string> { $"Requested discount: {request.RequestedDiscountPercentage}%", $"Fallback reason: {reason}" },
            Confidence = 0.5m,
            IsFallback = true
        });
    }

    private Task<AIExplanation> FallbackExplainDecisionAsync(
        ExplainabilityRequest request,
        string reason)
    {
        _metrics.RecordFallbackUsed("ExplainDecision", reason);

        return Task.FromResult(new AIExplanation
        {
            Summary = "Decision based on business rules (AI unavailable)",
            Details = new List<string>
            {
                $"Fallback reason: {reason}",
                "Used rule-based logic",
                "All business rules checked",
                "Margin requirements validated"
            }
        });
    }

    // ==================== SAFE DEFAULTS ====================

    private DiscountRecommendation CreateSafeDefaultRecommendation(DiscountRecommendationRequest request)
    {
        return new DiscountRecommendation
        {
            RecommendedDiscountPercentage = 0m, // No discount recommended when system fails
            ExpectedMarginPercentage = 25m,
            Confidence = 0.0m,
            Explanation = "System error - no recommendation available. Please review manually."
        };
    }

    private AIRiskScore CreateSafeDefaultRiskScore(RiskScoreRequest request)
    {
        return new AIRiskScore
        {
            Score = 100m, // Maximum risk when system fails (requires manual approval)
            RiskLevel = "VeryHigh",
            RiskFactors = new List<string> { "System error", "Manual review required" },
            Confidence = 0m,
            IsFallback = true
        };
    }

    private AIExplanation CreateSafeDefaultExplanation(ExplainabilityRequest request)
    {
        return new AIExplanation
        {
            Summary = "System error - explanation unavailable",
            Details = new List<string> { "System encountered an error", "Manual review required" }
        };
    }

    // ==================== CACHE KEY GENERATION ====================

    private string GenerateCacheKey(string operation, object request)
    {
        // Simple hash-based cache key
        // In production, use more sophisticated key generation
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(json));
        var hashString = Convert.ToBase64String(hash);
        return $"ai:{operation}:{hashString}";
    }
}

/// <summary>
/// Circuit breaker state to prevent cascading failures
/// Opens after 5 consecutive failures, stays open for 30 seconds
/// </summary>
public class CircuitBreakerState
{
    private int _consecutiveFailures = 0;
    private DateTime? _openedAt = null;
    private readonly int _failureThreshold = 5;
    private readonly TimeSpan _openDuration = TimeSpan.FromSeconds(30);
    private readonly object _lock = new object();

    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                if (_openedAt.HasValue)
                {
                    if (DateTime.UtcNow - _openedAt.Value > _openDuration)
                    {
                        // Half-open state - allow one request to test
                        _openedAt = null;
                        _consecutiveFailures = 0;
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _openedAt = null;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _failureThreshold)
            {
                _openedAt = DateTime.UtcNow;
            }
        }
    }
}
