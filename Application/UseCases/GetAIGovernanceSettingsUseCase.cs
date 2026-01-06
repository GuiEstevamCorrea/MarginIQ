using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case: Get AI Governance Settings (5.4 – Governança da IA)
/// Retrieves current AI governance configuration for a company.
/// Governance controls:
/// - Enable/disable AI per company
/// - Adjust autonomy level
/// - Configure auto-approval thresholds
/// - Audit settings
/// </summary>
public class GetAIGovernanceSettingsUseCase
{
    private readonly IAIService _aiService;
    private readonly ICompanyRepository _companyRepository;

    public GetAIGovernanceSettingsUseCase(
        IAIService aiService,
        ICompanyRepository companyRepository)
    {
        _aiService = aiService;
        _companyRepository = companyRepository;
    }

    /// <summary>
    /// Gets AI governance settings for a company
    /// </summary>
    public async Task<AIGovernanceResponse> ExecuteAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        // Validate company exists
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            throw new InvalidOperationException($"Company {companyId} not found");
        }

        // Get settings from AI service
        var settings = await _aiService.GetGovernanceSettingsAsync(companyId, cancellationToken);

        // Check AI availability
        var isAvailable = await _aiService.IsAvailableAsync(companyId, cancellationToken);

        // Map to response DTO
        var response = new AIGovernanceResponse
        {
            CompanyId = companyId,
            AIEnabled = settings.AIEnabled,
            AutonomyLevel = settings.AutonomyLevel,
            MaxRiskScoreForAutoApproval = settings.MaxRiskScoreForAutoApproval,
            MinConfidenceForAutoApproval = settings.MinConfidenceForAutoApproval,
            RequireHumanReview = settings.RequireHumanReview,
            EnableAudit = settings.EnableAudit,
            EnableExplainability = settings.EnableExplainability,
            MaxAutoApprovalDiscount = settings.MaxAutoApprovalDiscount,
            EnableIncrementalLearning = settings.EnableIncrementalLearning,
            RetrainingFrequencyDays = settings.RetrainingFrequencyDays,
            UpdatedAt = settings.UpdatedAt,
            Status = new AIOperationalStatus
            {
                IsAvailable = isAvailable,
                LastChecked = DateTime.UtcNow
            }
        };

        // Calculate next retraining date if incremental learning is enabled
        if (settings.EnableIncrementalLearning && settings.RetrainingFrequencyDays > 0)
        {
            response.NextRetrainingDate = settings.UpdatedAt.AddDays(settings.RetrainingFrequencyDays);
        }

        return response;
    }
}
