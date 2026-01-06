using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case: Update AI Governance Settings (5.4 – Governança da IA)
/// Updates AI governance configuration for a company.
/// Allows control over:
/// - Enabling/disabling AI
/// - Adjusting autonomy level (0-100)
/// - Configuring auto-approval thresholds
/// - Audit and explainability settings
/// - Incremental learning configuration
/// </summary>
public class UpdateAIGovernanceSettingsUseCase
{
    private readonly IAIService _aiService;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateAIGovernanceSettingsUseCase(
        IAIService aiService,
        ICompanyRepository companyRepository,
        IAuditLogRepository auditLogRepository)
    {
        _aiService = aiService;
        _companyRepository = companyRepository;
        _auditLogRepository = auditLogRepository;
    }

    /// <summary>
    /// Updates AI governance settings for a company
    /// </summary>
    public async Task<AIGovernanceResponse> ExecuteAsync(
        UpdateAIGovernanceRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate company exists
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company == null)
        {
            throw new InvalidOperationException($"Company {request.CompanyId} not found");
        }

        // Validate settings
        ValidateSettings(request);

        // Get current settings for audit trail
        var currentSettings = await _aiService.GetGovernanceSettingsAsync(
            request.CompanyId,
            cancellationToken);

        // Map request to AI governance settings
        var newSettings = new AIGovernanceSettings
        {
            AIEnabled = request.AIEnabled,
            AutonomyLevel = request.AutonomyLevel,
            MaxRiskScoreForAutoApproval = request.MaxRiskScoreForAutoApproval,
            MinConfidenceForAutoApproval = request.MinConfidenceForAutoApproval,
            RequireHumanReview = request.RequireHumanReview,
            EnableAudit = request.EnableAudit,
            EnableExplainability = request.EnableExplainability,
            MaxAutoApprovalDiscount = request.MaxAutoApprovalDiscount,
            EnableIncrementalLearning = request.EnableIncrementalLearning,
            RetrainingFrequencyDays = request.RetrainingFrequencyDays,
            UpdatedAt = DateTime.UtcNow
        };

        // Update settings via AI service
        await _aiService.UpdateGovernanceSettingsAsync(
            request.CompanyId,
            newSettings,
            cancellationToken);

        // Log governance change to audit trail
        await LogGovernanceChange(
            request.CompanyId,
            updatedByUserId,
            currentSettings,
            newSettings,
            cancellationToken);

        // Build response
        var response = new AIGovernanceResponse
        {
            CompanyId = request.CompanyId,
            AIEnabled = newSettings.AIEnabled,
            AutonomyLevel = newSettings.AutonomyLevel,
            MaxRiskScoreForAutoApproval = newSettings.MaxRiskScoreForAutoApproval,
            MinConfidenceForAutoApproval = newSettings.MinConfidenceForAutoApproval,
            RequireHumanReview = newSettings.RequireHumanReview,
            EnableAudit = newSettings.EnableAudit,
            EnableExplainability = newSettings.EnableExplainability,
            MaxAutoApprovalDiscount = newSettings.MaxAutoApprovalDiscount,
            EnableIncrementalLearning = newSettings.EnableIncrementalLearning,
            RetrainingFrequencyDays = newSettings.RetrainingFrequencyDays,
            UpdatedAt = newSettings.UpdatedAt,
            Status = new AIOperationalStatus
            {
                IsAvailable = newSettings.AIEnabled && await _aiService.IsAvailableAsync(
                    request.CompanyId,
                    cancellationToken),
                LastChecked = DateTime.UtcNow
            }
        };

        // Calculate next retraining date
        if (newSettings.EnableIncrementalLearning && newSettings.RetrainingFrequencyDays > 0)
        {
            response.NextRetrainingDate = newSettings.UpdatedAt.AddDays(newSettings.RetrainingFrequencyDays);
        }

        return response;
    }

    /// <summary>
    /// Validates governance settings
    /// </summary>
    private void ValidateSettings(UpdateAIGovernanceRequest request)
    {
        var errors = new List<string>();

        // Validate autonomy level
        if (request.AutonomyLevel < 0 || request.AutonomyLevel > 100)
        {
            errors.Add("Autonomy level must be between 0 and 100");
        }

        // Validate risk score threshold
        if (request.MaxRiskScoreForAutoApproval < 0 || request.MaxRiskScoreForAutoApproval > 100)
        {
            errors.Add("Max risk score for auto-approval must be between 0 and 100");
        }

        // Validate confidence threshold
        if (request.MinConfidenceForAutoApproval < 0 || request.MinConfidenceForAutoApproval > 1)
        {
            errors.Add("Min confidence for auto-approval must be between 0 and 1");
        }

        // Validate max discount
        if (request.MaxAutoApprovalDiscount < 0 || request.MaxAutoApprovalDiscount > 100)
        {
            errors.Add("Max auto-approval discount must be between 0 and 100");
        }

        // Validate retraining frequency
        if (request.EnableIncrementalLearning && request.RetrainingFrequencyDays <= 0)
        {
            errors.Add("Retraining frequency must be greater than 0 when incremental learning is enabled");
        }

        // Logical validations
        if (request.RequireHumanReview && request.AutonomyLevel > 50)
        {
            errors.Add("Autonomy level should be low (≤50) when requiring human review for all decisions");
        }

        if (!request.AIEnabled && request.AutonomyLevel > 0)
        {
            errors.Add("Autonomy level should be 0 when AI is disabled");
        }

        if (errors.Any())
        {
            throw new ArgumentException($"Invalid governance settings: {string.Join("; ", errors)}");
        }
    }

    /// <summary>
    /// Logs governance change to audit trail
    /// </summary>
    private async Task LogGovernanceChange(
        Guid companyId,
        Guid userId,
        AIGovernanceSettings oldSettings,
        AIGovernanceSettings newSettings,
        CancellationToken cancellationToken)
    {
        var changes = BuildChangeLog(oldSettings, newSettings);

        if (!changes.Any())
        {
            return; // No changes to log
        }

        var auditLog = AuditLog.CreateForHuman(
            entityName: "AIGovernanceSettings",
            entityId: companyId,
            action: Domain.Enums.AuditAction.Updated,
            companyId: companyId,
            userId: userId,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                Changes = changes,
                OldSettings = oldSettings,
                NewSettings = newSettings
            }));

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Builds list of changes between old and new settings
    /// </summary>
    private List<string> BuildChangeLog(
        AIGovernanceSettings oldSettings,
        AIGovernanceSettings newSettings)
    {
        var changes = new List<string>();

        if (oldSettings.AIEnabled != newSettings.AIEnabled)
        {
            changes.Add($"AI Enabled: {oldSettings.AIEnabled} → {newSettings.AIEnabled}");
        }

        if (oldSettings.AutonomyLevel != newSettings.AutonomyLevel)
        {
            changes.Add($"Autonomy Level: {oldSettings.AutonomyLevel} → {newSettings.AutonomyLevel}");
        }

        if (oldSettings.MaxRiskScoreForAutoApproval != newSettings.MaxRiskScoreForAutoApproval)
        {
            changes.Add($"Max Risk Score: {oldSettings.MaxRiskScoreForAutoApproval} → {newSettings.MaxRiskScoreForAutoApproval}");
        }

        if (oldSettings.MinConfidenceForAutoApproval != newSettings.MinConfidenceForAutoApproval)
        {
            changes.Add($"Min Confidence: {oldSettings.MinConfidenceForAutoApproval:P0} → {newSettings.MinConfidenceForAutoApproval:P0}");
        }

        if (oldSettings.RequireHumanReview != newSettings.RequireHumanReview)
        {
            changes.Add($"Require Human Review: {oldSettings.RequireHumanReview} → {newSettings.RequireHumanReview}");
        }

        if (oldSettings.EnableAudit != newSettings.EnableAudit)
        {
            changes.Add($"Enable Audit: {oldSettings.EnableAudit} → {newSettings.EnableAudit}");
        }

        if (oldSettings.EnableExplainability != newSettings.EnableExplainability)
        {
            changes.Add($"Enable Explainability: {oldSettings.EnableExplainability} → {newSettings.EnableExplainability}");
        }

        if (oldSettings.MaxAutoApprovalDiscount != newSettings.MaxAutoApprovalDiscount)
        {
            changes.Add($"Max Auto-Approval Discount: {oldSettings.MaxAutoApprovalDiscount}% → {newSettings.MaxAutoApprovalDiscount}%");
        }

        if (oldSettings.EnableIncrementalLearning != newSettings.EnableIncrementalLearning)
        {
            changes.Add($"Enable Incremental Learning: {oldSettings.EnableIncrementalLearning} → {newSettings.EnableIncrementalLearning}");
        }

        if (oldSettings.RetrainingFrequencyDays != newSettings.RetrainingFrequencyDays)
        {
            changes.Add($"Retraining Frequency: {oldSettings.RetrainingFrequencyDays} days → {newSettings.RetrainingFrequencyDays} days");
        }

        return changes;
    }
}
