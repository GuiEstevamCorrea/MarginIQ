using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for AI Learning Data.
/// Manages the learning database that stores historical decisions and outcomes
/// for AI training.
/// </summary>
public interface IAILearningDataRepository
{
    /// <summary>
    /// Gets a learning data point by ID
    /// </summary>
    /// <param name="id">Learning data ID</param>
    /// <returns>Learning data if found, null otherwise</returns>
    Task<AILearningData?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets learning data by discount request ID
    /// </summary>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Learning data if found, null otherwise</returns>
    Task<AILearningData?> GetByDiscountRequestIdAsync(Guid discountRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all learning data for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of learning data points</returns>
    Task<IEnumerable<AILearningData>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 100);

    /// <summary>
    /// Gets learning data ready for training (complete and not yet used)
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="maxAgeDays">Maximum age of data in days (for recent data only)</param>
    /// <returns>List of learning data points ready for training</returns>
    Task<IEnumerable<AILearningData>> GetReadyForTrainingAsync(Guid companyId, int? maxAgeDays = null);

    /// <summary>
    /// Gets learning data that has been used for training
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of learning data points used for training</returns>
    Task<IEnumerable<AILearningData>> GetUsedForTrainingAsync(Guid companyId, int skip = 0, int take = 100);

    /// <summary>
    /// Gets learning data by decision source (Human or AI)
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="source">Decision source</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of learning data points</returns>
    Task<IEnumerable<AILearningData>> GetByDecisionSourceAsync(
        Guid companyId, 
        Domain.Enums.ApprovalSource source, 
        int skip = 0, 
        int take = 100);

    /// <summary>
    /// Gets learning data with sale outcomes (won/lost)
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="won">True for won sales, false for lost sales, null for all with outcomes</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of learning data points with outcomes</returns>
    Task<IEnumerable<AILearningData>> GetWithSaleOutcomeAsync(
        Guid companyId, 
        bool? won = null, 
        int skip = 0, 
        int take = 100);

    /// <summary>
    /// Gets learning data by customer
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="customerId">Customer ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of learning data points for the customer</returns>
    Task<IEnumerable<AILearningData>> GetByCustomerAsync(
        Guid companyId, 
        Guid customerId, 
        int skip = 0, 
        int take = 100);

    /// <summary>
    /// Gets learning data by salesperson
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="salespersonId">Salesperson ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of learning data points for the salesperson</returns>
    Task<IEnumerable<AILearningData>> GetBySalespersonAsync(
        Guid companyId, 
        Guid salespersonId, 
        int skip = 0, 
        int take = 100);

    /// <summary>
    /// Gets learning data by date range
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of learning data points in the date range</returns>
    Task<IEnumerable<AILearningData>> GetByDateRangeAsync(
        Guid companyId, 
        DateTime startDate, 
        DateTime endDate);

    /// <summary>
    /// Adds a new learning data point
    /// </summary>
    /// <param name="learningData">Learning data to add</param>
    Task AddAsync(AILearningData learningData);

    /// <summary>
    /// Adds multiple learning data points in batch
    /// </summary>
    /// <param name="learningDataList">List of learning data to add</param>
    Task AddRangeAsync(IEnumerable<AILearningData> learningDataList);

    /// <summary>
    /// Updates an existing learning data point
    /// </summary>
    /// <param name="learningData">Learning data to update</param>
    Task UpdateAsync(AILearningData learningData);

    /// <summary>
    /// Gets training statistics for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <returns>Training statistics</returns>
    Task<AILearningStatistics> GetStatisticsAsync(Guid companyId);

    /// <summary>
    /// Archives old learning data (marks as archived, not deleted)
    /// Useful for data retention policies
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="olderThanDays">Archive data older than this many days</param>
    /// <returns>Number of records archived</returns>
    Task<int> ArchiveOldDataAsync(Guid companyId, int olderThanDays);

    /// <summary>
    /// Gets count of learning data points for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="completeOnly">Count only complete data (with sale outcomes)</param>
    /// <returns>Count of learning data points</returns>
    Task<int> GetCountAsync(Guid companyId, bool completeOnly = false);
}

/// <summary>
/// Statistics about AI learning data for a company
/// </summary>
public class AILearningStatistics
{
    /// <summary>
    /// Total number of learning data points
    /// </summary>
    public int TotalDataPoints { get; set; }

    /// <summary>
    /// Number of complete data points (with sale outcomes)
    /// </summary>
    public int CompleteDataPoints { get; set; }

    /// <summary>
    /// Number of data points used for training
    /// </summary>
    public int UsedForTraining { get; set; }

    /// <summary>
    /// Number of data points ready for training
    /// </summary>
    public int ReadyForTraining { get; set; }

    /// <summary>
    /// Number of approved decisions
    /// </summary>
    public int ApprovedDecisions { get; set; }

    /// <summary>
    /// Number of rejected decisions
    /// </summary>
    public int RejectedDecisions { get; set; }

    /// <summary>
    /// Number of AI decisions
    /// </summary>
    public int AIDecisions { get; set; }

    /// <summary>
    /// Number of human decisions
    /// </summary>
    public int HumanDecisions { get; set; }

    /// <summary>
    /// Number of won sales
    /// </summary>
    public int WonSales { get; set; }

    /// <summary>
    /// Number of lost sales
    /// </summary>
    public int LostSales { get; set; }

    /// <summary>
    /// Win rate (percentage)
    /// </summary>
    public decimal WinRate { get; set; }

    /// <summary>
    /// Average discount percentage
    /// </summary>
    public decimal AverageDiscount { get; set; }

    /// <summary>
    /// Average margin percentage
    /// </summary>
    public decimal AverageMargin { get; set; }

    /// <summary>
    /// Average risk score
    /// </summary>
    public decimal AverageRiskScore { get; set; }

    /// <summary>
    /// Date of oldest data point
    /// </summary>
    public DateTime? OldestDataDate { get; set; }

    /// <summary>
    /// Date of newest data point
    /// </summary>
    public DateTime? NewestDataDate { get; set; }

    /// <summary>
    /// Last training date
    /// </summary>
    public DateTime? LastTrainingDate { get; set; }
}
