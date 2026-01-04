using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents a Product in the system.
/// Products belong to a Company (multi-tenant) and are used in discount requests.
/// The AI uses product pricing and margin history to recommend discounts.
/// </summary>
public class Product
{
    /// <summary>
    /// Unique identifier for the product
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Product name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Product category (e.g., Hardware, Software, Services)
    /// Used for applying category-level discount rules
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Base price of the product (before discounts)
    /// </summary>
    public Money BasePrice { get; private set; }

    /// <summary>
    /// Base margin percentage (0-100)
    /// Used as reference for discount validation and AI learning
    /// </summary>
    public decimal BaseMarginPercentage { get; private set; }

    /// <summary>
    /// The company (tenant) this product belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Current status of the product
    /// </summary>
    public ProductStatus Status { get; private set; }

    /// <summary>
    /// Product SKU or code (optional identifier)
    /// </summary>
    public string? Sku { get; private set; }

    /// <summary>
    /// Additional product information stored as JSON
    /// </summary>
    public string? AdditionalInfo { get; private set; }

    /// <summary>
    /// Date and time when the product was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the product was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation property
    public Company? Company { get; private set; }

    // Private constructor for EF Core
    private Product() 
    {
        Name = string.Empty;
        BasePrice = Money.Zero();
    }

    /// <summary>
    /// Creates a new Product instance
    /// </summary>
    /// <param name="name">Product name</param>
    /// <param name="basePrice">Base price before discounts</param>
    /// <param name="baseMarginPercentage">Base margin percentage (0-100)</param>
    /// <param name="companyId">The company this product belongs to</param>
    /// <param name="category">Product category (optional)</param>
    /// <param name="sku">Product SKU/code (optional)</param>
    public Product(
        string name, 
        Money basePrice, 
        decimal baseMarginPercentage, 
        Guid companyId,
        string? category = null,
        string? sku = null)
    {
        ValidateName(name);
        ValidateBaseMarginPercentage(baseMarginPercentage);

        Id = Guid.NewGuid();
        Name = name;
        BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
        BaseMarginPercentage = baseMarginPercentage;
        CompanyId = companyId;
        Category = category;
        Sku = sku;
        Status = ProductStatus.Active; // New products start as active
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the product
    /// </summary>
    public void Activate()
    {
        if (Status == ProductStatus.Active)
            throw new InvalidOperationException("Product is already active");

        Status = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the product (temporarily unavailable)
    /// </summary>
    public void Deactivate()
    {
        if (Status == ProductStatus.Inactive)
            throw new InvalidOperationException("Product is already inactive");

        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the product as discontinued (permanently unavailable)
    /// </summary>
    public void Discontinue()
    {
        if (Status == ProductStatus.Discontinued)
            throw new InvalidOperationException("Product is already discontinued");

        Status = ProductStatus.Discontinued;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the product name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the product category
    /// </summary>
    /// <param name="category">New category</param>
    public void UpdateCategory(string? category)
    {
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the base price
    /// </summary>
    /// <param name="basePrice">New base price</param>
    public void UpdateBasePrice(Money basePrice)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        BasePrice = basePrice;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the base margin percentage
    /// </summary>
    /// <param name="baseMarginPercentage">New base margin percentage (0-100)</param>
    public void UpdateBaseMarginPercentage(decimal baseMarginPercentage)
    {
        ValidateBaseMarginPercentage(baseMarginPercentage);
        BaseMarginPercentage = baseMarginPercentage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the product SKU
    /// </summary>
    /// <param name="sku">New SKU</param>
    public void UpdateSku(string? sku)
    {
        Sku = sku;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the additional product information
    /// </summary>
    /// <param name="additionalInfo">New additional information JSON</param>
    public void UpdateAdditionalInfo(string? additionalInfo)
    {
        AdditionalInfo = additionalInfo;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the product is active
    /// </summary>
    /// <returns>True if the product is active, false otherwise</returns>
    public bool IsActive() => Status == ProductStatus.Active;

    /// <summary>
    /// Checks if the product is discontinued
    /// </summary>
    /// <returns>True if the product is discontinued, false otherwise</returns>
    public bool IsDiscontinued() => Status == ProductStatus.Discontinued;

    /// <summary>
    /// Checks if the product can be included in new discount requests
    /// </summary>
    /// <returns>True if product can be in discount requests, false otherwise</returns>
    public bool CanBeIncludedInDiscountRequests() => Status == ProductStatus.Active;

    /// <summary>
    /// Calculates the final price after applying a discount percentage
    /// </summary>
    /// <param name="discountPercentage">Discount percentage to apply (0-100)</param>
    /// <returns>Final price after discount</returns>
    public Money CalculatePriceAfterDiscount(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        var discountMultiplier = 1 - (discountPercentage / 100);
        return BasePrice * discountMultiplier;
    }

    /// <summary>
    /// Calculates the estimated margin after applying a discount
    /// </summary>
    /// <param name="discountPercentage">Discount percentage to apply (0-100)</param>
    /// <returns>Estimated margin percentage after discount</returns>
    public decimal CalculateMarginAfterDiscount(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        // Simplified calculation: base margin reduces proportionally with discount
        // More complex calculations can be done in domain services
        var marginReduction = discountPercentage * 0.5m; // Simplified factor
        var estimatedMargin = BaseMarginPercentage - marginReduction;
        
        return Math.Max(0, estimatedMargin); // Margin cannot be negative
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (name.Length < 2)
            throw new ArgumentException("Product name must have at least 2 characters", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));
    }

    private static void ValidateBaseMarginPercentage(decimal baseMarginPercentage)
    {
        if (baseMarginPercentage < 0 || baseMarginPercentage > 100)
            throw new ArgumentException("Base margin percentage must be between 0 and 100", nameof(baseMarginPercentage));
    }
}
