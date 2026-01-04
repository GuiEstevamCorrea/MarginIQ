using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a Company (Tenant) in the SaaS system.
/// All data in the system is isolated by Company (multi-tenant architecture).
/// The AI learns per company (logically separated model).
/// </summary>
public class Company
{
    /// <summary>
    /// Unique identifier for the company
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Company name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Business segment of the company (e.g., Manufacturing, SaaS, Distribution)
    /// </summary>
    public CompanySegment Segment { get; private set; }

    /// <summary>
    /// Current status of the company in the system
    /// </summary>
    public CompanyStatus Status { get; private set; }

    /// <summary>
    /// General configuration settings stored as JSON
    /// </summary>
    public string? GeneralSettings { get; private set; }

    /// <summary>
    /// Date and time when the company was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the company was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core
    private Company() { }

    /// <summary>
    /// Creates a new Company instance
    /// </summary>
    /// <param name="name">Company name</param>
    /// <param name="segment">Business segment</param>
    /// <param name="generalSettings">Optional general settings JSON</param>
    public Company(string name, CompanySegment segment, string? generalSettings = null)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name;
        Segment = segment;
        Status = CompanyStatus.Trial; // New companies start in trial
        GeneralSettings = generalSettings;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the company
    /// </summary>
    public void Activate()
    {
        if (Status == CompanyStatus.Active)
            throw new InvalidOperationException("Company is already active");

        Status = CompanyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the company
    /// </summary>
    public void Deactivate()
    {
        if (Status == CompanyStatus.Inactive)
            throw new InvalidOperationException("Company is already inactive");

        Status = CompanyStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends the company
    /// </summary>
    public void Suspend()
    {
        if (Status == CompanyStatus.Suspended)
            throw new InvalidOperationException("Company is already suspended");

        Status = CompanyStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the company name
    /// </summary>
    /// <param name="name">New company name</param>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the company segment
    /// </summary>
    /// <param name="segment">New business segment</param>
    public void UpdateSegment(CompanySegment segment)
    {
        Segment = segment;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the general settings
    /// </summary>
    /// <param name="settings">New settings JSON</param>
    public void UpdateGeneralSettings(string? settings)
    {
        GeneralSettings = settings;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the company is active and can perform operations
    /// </summary>
    /// <returns>True if the company is active, false otherwise</returns>
    public bool IsActive() => Status == CompanyStatus.Active;

    /// <summary>
    /// Checks if the company is in trial mode
    /// </summary>
    /// <returns>True if the company is in trial, false otherwise</returns>
    public bool IsTrial() => Status == CompanyStatus.Trial;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty", nameof(name));

        if (name.Length < 2)
            throw new ArgumentException("Company name must have at least 2 characters", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Company name cannot exceed 200 characters", nameof(name));
    }
}
