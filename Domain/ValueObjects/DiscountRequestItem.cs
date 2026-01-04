using Domain.ValueObjects;

namespace Domain.ValueObjects;

/// <summary>
/// Value object representing an item in a discount request
/// Contains product information, quantity, and pricing details
/// </summary>
public class DiscountRequestItem : IEquatable<DiscountRequestItem>
{
    /// <summary>
    /// Product ID
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Product name (denormalized for historical reference)
    /// </summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// Quantity of the product
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Base price per unit (before discount)
    /// </summary>
    public Money UnitBasePrice { get; private set; }

    /// <summary>
    /// Unit price after discount
    /// </summary>
    public Money UnitFinalPrice { get; private set; }

    /// <summary>
    /// Discount percentage applied to this item (0-100)
    /// </summary>
    public decimal DiscountPercentage { get; private set; }

    // Private constructor for EF Core
    private DiscountRequestItem() 
    {
        ProductName = string.Empty;
        UnitBasePrice = Money.Zero();
        UnitFinalPrice = Money.Zero();
    }

    /// <summary>
    /// Creates a new DiscountRequestItem instance
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="productName">Product name</param>
    /// <param name="quantity">Quantity</param>
    /// <param name="unitBasePrice">Base price per unit</param>
    /// <param name="discountPercentage">Discount percentage (0-100)</param>
    public DiscountRequestItem(
        Guid productId,
        string productName,
        int quantity,
        Money unitBasePrice,
        decimal discountPercentage)
    {
        ValidateProductName(productName);
        ValidateQuantity(quantity);
        ValidateDiscountPercentage(discountPercentage);

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitBasePrice = unitBasePrice ?? throw new ArgumentNullException(nameof(unitBasePrice));
        DiscountPercentage = discountPercentage;
        UnitFinalPrice = CalculateFinalPrice(unitBasePrice, discountPercentage);
    }

    /// <summary>
    /// Gets the total base price (quantity * unit base price)
    /// </summary>
    public Money GetTotalBasePrice() => UnitBasePrice * Quantity;

    /// <summary>
    /// Gets the total final price (quantity * unit final price)
    /// </summary>
    public Money GetTotalFinalPrice() => UnitFinalPrice * Quantity;

    /// <summary>
    /// Gets the total discount amount
    /// </summary>
    public Money GetTotalDiscountAmount() => GetTotalBasePrice() - GetTotalFinalPrice();

    /// <summary>
    /// Updates the discount percentage and recalculates the final price
    /// </summary>
    public DiscountRequestItem WithDiscountPercentage(decimal newDiscountPercentage)
    {
        ValidateDiscountPercentage(newDiscountPercentage);
        return new DiscountRequestItem(ProductId, ProductName, Quantity, UnitBasePrice, newDiscountPercentage);
    }

    /// <summary>
    /// Updates the quantity
    /// </summary>
    public DiscountRequestItem WithQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        return new DiscountRequestItem(ProductId, ProductName, newQuantity, UnitBasePrice, DiscountPercentage);
    }

    private static Money CalculateFinalPrice(Money basePrice, decimal discountPercentage)
    {
        var discountMultiplier = 1 - (discountPercentage / 100);
        return basePrice * discountMultiplier;
    }

    private static void ValidateProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
    }

    private static void ValidateDiscountPercentage(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));
    }

    public bool Equals(DiscountRequestItem? other)
    {
        if (other is null) return false;
        return ProductId == other.ProductId &&
               Quantity == other.Quantity &&
               DiscountPercentage == other.DiscountPercentage;
    }

    public override bool Equals(object? obj)
    {
        return obj is DiscountRequestItem item && Equals(item);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProductId, Quantity, DiscountPercentage);
    }

    public override string ToString()
    {
        return $"{ProductName} - Qty: {Quantity}, Discount: {DiscountPercentage:N2}%";
    }
}
