namespace Application.DTOs;

/// <summary>
/// Request DTO for triggering incremental learning (IA-UC-04)
/// </summary>
public class TriggerIncrementalLearningRequest
{
    /// <summary>
    /// Company ID (tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Training type: Incremental or Full
    /// </summary>
    public TrainingMode Mode { get; set; } = TrainingMode.Incremental;

    /// <summary>
    /// Date range for training data (optional - if not provided, uses all new data since last training)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for training data (optional - defaults to now)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Include only completed sales (with outcome) or also pending decisions
    /// </summary>
    public bool OnlyCompletedSales { get; set; } = true;

    /// <summary>
    /// Include human decisions in training
    /// </summary>
    public bool IncludeHumanDecisions { get; set; } = true;

    /// <summary>
    /// Include AI decisions in training
    /// </summary>
    public bool IncludeAIDecisions { get; set; } = false;

    /// <summary>
    /// Minimum number of data points required to trigger training
    /// </summary>
    public int MinimumDataPoints { get; set; } = 10;

    /// <summary>
    /// Force training even if minimum data points not met
    /// </summary>
    public bool ForceTraining { get; set; } = false;

    /// <summary>
    /// Use AI service for training (if false, simulates training without actual ML)
    /// </summary>
    public bool UseAI { get; set; } = true;
}

/// <summary>
/// Training mode
/// </summary>
public enum TrainingMode
{
    /// <summary>
    /// Incremental learning with new data only
    /// </summary>
    Incremental,

    /// <summary>
    /// Full model retraining with all historical data
    /// </summary>
    Full
}

/// <summary>
/// Response DTO for incremental learning (IA-UC-04)
/// </summary>
public class TriggerIncrementalLearningResponse
{
    /// <summary>
    /// Indicates if training was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Detailed message about training result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of data points collected for training
    /// </summary>
    public int DataPointsCollected { get; set; }

    /// <summary>
    /// Number of data points actually used in training
    /// </summary>
    public int DataPointsProcessed { get; set; }

    /// <summary>
    /// Training mode used
    /// </summary>
    public TrainingMode Mode { get; set; }

    /// <summary>
    /// Training breakdown by decision source
    /// </summary>
    public TrainingBreakdown Breakdown { get; set; } = new();

    /// <summary>
    /// Model version after training (if applicable)
    /// </summary>
    public string? ModelVersion { get; set; }

    /// <summary>
    /// Timestamp when training was executed
    /// </summary>
    public DateTime TrainedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of training process
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Indicates if AI was used (false means simulation/logging only)
    /// </summary>
    public bool UsedAI { get; set; }

    /// <summary>
    /// Warnings or issues encountered during training
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Training metrics (optional - depends on AI implementation)
    /// </summary>
    public TrainingMetrics? Metrics { get; set; }
}

/// <summary>
/// Breakdown of training data by source
/// </summary>
public class TrainingBreakdown
{
    /// <summary>
    /// Number of human decisions included
    /// </summary>
    public int HumanDecisions { get; set; }

    /// <summary>
    /// Number of AI decisions included
    /// </summary>
    public int AIDecisions { get; set; }

    /// <summary>
    /// Number with completed sale outcomes (won/lost)
    /// </summary>
    public int WithSaleOutcome { get; set; }

    /// <summary>
    /// Number with approved decisions
    /// </summary>
    public int Approved { get; set; }

    /// <summary>
    /// Number with rejected decisions
    /// </summary>
    public int Rejected { get; set; }

    /// <summary>
    /// Number of wins (sales closed)
    /// </summary>
    public int SalesWon { get; set; }

    /// <summary>
    /// Number of losses (sales lost)
    /// </summary>
    public int SalesLost { get; set; }
}

/// <summary>
/// Training metrics (AI-specific)
/// </summary>
public class TrainingMetrics
{
    /// <summary>
    /// Model accuracy after training (if available)
    /// </summary>
    public decimal? Accuracy { get; set; }

    /// <summary>
    /// Model confidence score
    /// </summary>
    public decimal? Confidence { get; set; }

    /// <summary>
    /// Improvement over previous model (percentage)
    /// </summary>
    public decimal? Improvement { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public decimal? ErrorRate { get; set; }

    /// <summary>
    /// Additional metrics as key-value pairs
    /// </summary>
    public Dictionary<string, decimal> AdditionalMetrics { get; set; } = new();
}
