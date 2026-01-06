using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a Customer in the system.
/// Customers belong to a Company (multi-tenant) and are the target of discount requests.
/// The AI uses customer history to recommend discounts and calculate risk scores.
/// </summary>
public class Customer
{
    /// <summary>
    /// Unique identifier for the customer
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Customer's name or company name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Business segment of the customer (e.g., retail, manufacturing, technology)
    /// </summary>
    public string? Segment { get; private set; }

    /// <summary>
    /// Customer classification/tier (A/B/C) - optional
    /// Used for segmentation and differentiated discount policies
    /// </summary>
    public CustomerClassification Classification { get; private set; }

    /// <summary>
    /// The company (tenant) this customer belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Current status of the customer
    /// </summary>
    public CustomerStatus Status { get; private set; }

    /// <summary>
    /// Additional customer information (tax ID, address, etc.) stored as JSON
    /// </summary>
    public string? AdditionalInfo { get; private set; }

    /// <summary>
    /// External system identifier for integration (SAP, TOTVS, etc.)
    /// Used to sync data with external ERP/CRM systems
    /// </summary>
    public string? ExternalSystemId { get; private set; }

    /// <summary>
    /// Date and time when the customer was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the customer was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation property
    public Company? Company { get; private set; }

    // Private constructor for EF Core
    private Customer() { }

    /// <summary>
    /// Creates a new Customer instance
    /// </summary>
    /// <param name="name">Customer's name</param>
    /// <param name="companyId">The company this customer belongs to</param>
    /// <param name="segment">Business segment (optional)</param>
    /// <param name="classification">Customer classification (optional, defaults to Unclassified)</param>
    public Customer(string name, Guid companyId, string? segment = null, CustomerClassification classification = CustomerClassification.Unclassified)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name;
        CompanyId = companyId;
        Segment = segment;
        Classification = classification;
        Status = CustomerStatus.Prospect; // New customers start as prospects
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the customer
    /// </summary>
    public void Activate()
    {
        if (Status == CustomerStatus.Active)
            throw new InvalidOperationException("Customer is already active");

        if (Status == CustomerStatus.Blocked)
            throw new InvalidOperationException("Cannot activate a blocked customer. Unblock first.");

        Status = CustomerStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the customer
    /// </summary>
    public void Deactivate()
    {
        if (Status == CustomerStatus.Inactive)
            throw new InvalidOperationException("Customer is already inactive");

        Status = CustomerStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Blocks the customer (blocked customers cannot receive new discount requests)
    /// </summary>
    public void Block()
    {
        if (Status == CustomerStatus.Blocked)
            throw new InvalidOperationException("Customer is already blocked");

        Status = CustomerStatus.Blocked;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unblocks the customer
    /// </summary>
    public void Unblock()
    {
        if (Status != CustomerStatus.Blocked)
            throw new InvalidOperationException("Customer is not blocked");

        Status = CustomerStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Promotes a prospect to an active customer
    /// </summary>
    public void PromoteToActive()
    {
        if (Status != CustomerStatus.Prospect)
            throw new InvalidOperationException("Only prospects can be promoted to active");

        Status = CustomerStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the customer's name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the customer's segment
    /// </summary>
    /// <param name="segment">New segment</param>
    public void UpdateSegment(string? segment)
    {
        Segment = segment;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the customer's classification
    /// </summary>
    /// <param name="classification">New classification</param>
    public void UpdateClassification(CustomerClassification classification)
    {
        Classification = classification;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the customer's additional information
    /// </summary>
    /// <param name="additionalInfo">New additional information JSON</param>
    public void UpdateAdditionalInfo(string? additionalInfo)
    {
        AdditionalInfo = additionalInfo;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the external system identifier for integration purposes
    /// </summary>
    /// <param name="externalSystemId">External system ID (from SAP, TOTVS, etc.)</param>
    public void SetExternalId(string? externalSystemId)
    {
        ExternalSystemId = externalSystemId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the customer is active
    /// </summary>
    /// <returns>True if the customer is active, false otherwise</returns>
    public bool IsActive() => Status == CustomerStatus.Active;

    /// <summary>
    /// Checks if the customer is blocked
    /// </summary>
    /// <returns>True if the customer is blocked, false otherwise</returns>
    public bool IsBlocked() => Status == CustomerStatus.Blocked;

    /// <summary>
    /// Checks if the customer is a prospect
    /// </summary>
    /// <returns>True if the customer is a prospect, false otherwise</returns>
    public bool IsProspect() => Status == CustomerStatus.Prospect;

    /// <summary>
    /// Checks if the customer can receive discount requests
    /// Business rule: blocked customers cannot receive new discount requests
    /// </summary>
    /// <returns>True if customer can receive discount requests, false otherwise</returns>
    public bool CanReceiveDiscountRequests() => Status == CustomerStatus.Active || Status == CustomerStatus.Prospect;

    /// <summary>
    /// Checks if the customer is classified as A-tier (top tier)
    /// </summary>
    /// <returns>True if classification is A, false otherwise</returns>
    public bool IsClassificationA() => Classification == CustomerClassification.A;

    /// <summary>
    /// Checks if the customer is classified as B-tier (mid tier)
    /// </summary>
    /// <returns>True if classification is B, false otherwise</returns>
    public bool IsClassificationB() => Classification == CustomerClassification.B;

    /// <summary>
    /// Checks if the customer is classified as C-tier (lower tier)
    /// </summary>
    /// <returns>True if classification is C, false otherwise</returns>
    public bool IsClassificationC() => Classification == CustomerClassification.C;

    /// <summary>
    /// Checks if the customer is unclassified
    /// </summary>
    /// <returns>True if classification is Unclassified, false otherwise</returns>
    public bool IsUnclassified() => Classification == CustomerClassification.Unclassified;

    /// <summary>
    /// Gets the classification tier as a string
    /// </summary>
    /// <returns>Classification as string (A, B, C, or Unclassified)</returns>
    public string GetClassificationTier()
    {
        return Classification switch
        {
            CustomerClassification.A => "A",
            CustomerClassification.B => "B",
            CustomerClassification.C => "C",
            CustomerClassification.Unclassified => "Unclassified",
            _ => "Unknown"
        };
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty", nameof(name));

        if (name.Length < 2)
            throw new ArgumentException("Customer name must have at least 2 characters", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Customer name cannot exceed 200 characters", nameof(name));
    }
}
