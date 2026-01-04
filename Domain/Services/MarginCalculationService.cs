using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Domain service for margin calculations.
/// Implements business rules for calculating profit margins on discount requests.
/// Formula: margin = (finalPrice - estimatedCost) / finalPrice
/// </summary>
public class MarginCalculationService
{
    /// <summary>
    /// Calculates the margin percentage based on final price and estimated cost
    /// Formula: margin = (finalPrice - estimatedCost) / finalPrice
    /// </summary>
    /// <param name="finalPrice">Final price after discount</param>
    /// <param name="estimatedCost">Estimated cost of the product/service</param>
    /// <returns>Margin percentage (0-100)</returns>
    /// <exception cref="ArgumentNullException">When finalPrice or estimatedCost is null</exception>
    /// <exception cref="ArgumentException">When finalPrice is zero or negative</exception>
    public decimal CalculateMarginPercentage(Money finalPrice, Money estimatedCost)
    {
        if (finalPrice == null)
            throw new ArgumentNullException(nameof(finalPrice));

        if (estimatedCost == null)
            throw new ArgumentNullException(nameof(estimatedCost));

        if (finalPrice.Value <= 0)
            throw new ArgumentException("Final price must be greater than zero", nameof(finalPrice));

        // Validate same currency
        if (finalPrice.Currency != estimatedCost.Currency)
            throw new InvalidOperationException($"Cannot calculate margin with different currencies: {finalPrice.Currency} and {estimatedCost.Currency}");

        // margin = (finalPrice - estimatedCost) / finalPrice
        var marginValue = (finalPrice.Value - estimatedCost.Value) / finalPrice.Value;

        // Convert to percentage (0-100)
        var marginPercentage = marginValue * 100;

        return Math.Round(marginPercentage, 2);
    }

    /// <summary>
    /// Calculates the margin percentage after applying a discount
    /// </summary>
    /// <param name="basePrice">Base price before discount</param>
    /// <param name="estimatedCost">Estimated cost of the product/service</param>
    /// <param name="discountPercentage">Discount percentage to apply (0-100)</param>
    /// <returns>Margin percentage after discount (0-100)</returns>
    public decimal CalculateMarginAfterDiscount(Money basePrice, Money estimatedCost, decimal discountPercentage)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        if (estimatedCost == null)
            throw new ArgumentNullException(nameof(estimatedCost));

        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));

        // Calculate final price after discount
        var discountMultiplier = 1 - (discountPercentage / 100);
        var finalPrice = basePrice * discountMultiplier;

        return CalculateMarginPercentage(finalPrice, estimatedCost);
    }

    /// <summary>
    /// Calculates the profit amount (finalPrice - estimatedCost)
    /// </summary>
    /// <param name="finalPrice">Final price after discount</param>
    /// <param name="estimatedCost">Estimated cost of the product/service</param>
    /// <returns>Profit amount</returns>
    public Money CalculateProfitAmount(Money finalPrice, Money estimatedCost)
    {
        if (finalPrice == null)
            throw new ArgumentNullException(nameof(finalPrice));

        if (estimatedCost == null)
            throw new ArgumentNullException(nameof(estimatedCost));

        return finalPrice - estimatedCost;
    }

    /// <summary>
    /// Validates if the margin meets the minimum requirement
    /// </summary>
    /// <param name="actualMarginPercentage">Actual margin percentage</param>
    /// <param name="minimumMarginPercentage">Minimum required margin percentage</param>
    /// <returns>True if margin meets minimum requirement, false otherwise</returns>
    public bool IsMarginAboveMinimum(decimal actualMarginPercentage, decimal minimumMarginPercentage)
    {
        return actualMarginPercentage >= minimumMarginPercentage;
    }

    /// <summary>
    /// Calculates the estimated cost based on final price and desired margin
    /// Useful for reverse calculation: cost = finalPrice * (1 - desiredMargin)
    /// </summary>
    /// <param name="finalPrice">Final price</param>
    /// <param name="desiredMarginPercentage">Desired margin percentage (0-100)</param>
    /// <returns>Estimated cost</returns>
    public Money CalculateEstimatedCost(Money finalPrice, decimal desiredMarginPercentage)
    {
        if (finalPrice == null)
            throw new ArgumentNullException(nameof(finalPrice));

        if (desiredMarginPercentage < 0 || desiredMarginPercentage > 100)
            throw new ArgumentException("Margin percentage must be between 0 and 100", nameof(desiredMarginPercentage));

        // cost = finalPrice * (1 - margin)
        var costMultiplier = 1 - (desiredMarginPercentage / 100);
        return finalPrice * costMultiplier;
    }

    /// <summary>
    /// Calculates the maximum discount percentage that maintains a minimum margin
    /// </summary>
    /// <param name="basePrice">Base price before discount</param>
    /// <param name="estimatedCost">Estimated cost of the product/service</param>
    /// <param name="minimumMarginPercentage">Minimum required margin percentage (0-100)</param>
    /// <returns>Maximum discount percentage that maintains minimum margin</returns>
    public decimal CalculateMaxDiscountForMinimumMargin(Money basePrice, Money estimatedCost, decimal minimumMarginPercentage)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        if (estimatedCost == null)
            throw new ArgumentNullException(nameof(estimatedCost));

        if (minimumMarginPercentage < 0 || minimumMarginPercentage > 100)
            throw new ArgumentException("Margin percentage must be between 0 and 100", nameof(minimumMarginPercentage));

        // Calculate the minimum acceptable final price
        // margin = (finalPrice - cost) / finalPrice
        // finalPrice * margin = finalPrice - cost
        // finalPrice * (1 - margin) = cost
        // finalPrice = cost / (1 - margin)
        var marginDecimal = minimumMarginPercentage / 100;
        var minimumFinalPriceValue = estimatedCost.Value / (1 - marginDecimal);

        // Calculate max discount percentage
        // discount = (basePrice - minimumFinalPrice) / basePrice
        var maxDiscountDecimal = (basePrice.Value - minimumFinalPriceValue) / basePrice.Value;
        var maxDiscountPercentage = maxDiscountDecimal * 100;

        // Ensure discount is not negative
        return Math.Max(0, Math.Round(maxDiscountPercentage, 2));
    }

    /// <summary>
    /// Calculates the margin impact of a discount
    /// Returns how much the margin will decrease if the discount is applied
    /// </summary>
    /// <param name="basePrice">Base price before discount</param>
    /// <param name="estimatedCost">Estimated cost</param>
    /// <param name="discountPercentage">Discount percentage (0-100)</param>
    /// <returns>Margin decrease in percentage points</returns>
    public decimal CalculateMarginImpact(Money basePrice, Money estimatedCost, decimal discountPercentage)
    {
        var marginBeforeDiscount = CalculateMarginPercentage(basePrice, estimatedCost);
        var marginAfterDiscount = CalculateMarginAfterDiscount(basePrice, estimatedCost, discountPercentage);

        return marginBeforeDiscount - marginAfterDiscount;
    }

    /// <summary>
    /// Validates if a discount request maintains the minimum margin requirement
    /// </summary>
    /// <param name="basePrice">Base price before discount</param>
    /// <param name="estimatedCost">Estimated cost</param>
    /// <param name="discountPercentage">Requested discount percentage (0-100)</param>
    /// <param name="minimumMarginPercentage">Minimum required margin percentage (0-100)</param>
    /// <returns>True if discount maintains minimum margin, false otherwise</returns>
    public bool ValidateMinimumMargin(Money basePrice, Money estimatedCost, decimal discountPercentage, decimal minimumMarginPercentage)
    {
        var marginAfterDiscount = CalculateMarginAfterDiscount(basePrice, estimatedCost, discountPercentage);
        return IsMarginAboveMinimum(marginAfterDiscount, minimumMarginPercentage);
    }

    /// <summary>
    /// Calculates margin for multiple items (aggregate calculation)
    /// </summary>
    /// <param name="totalFinalPrice">Sum of all final prices</param>
    /// <param name="totalEstimatedCost">Sum of all estimated costs</param>
    /// <returns>Overall margin percentage</returns>
    public decimal CalculateAggregateMargin(Money totalFinalPrice, Money totalEstimatedCost)
    {
        return CalculateMarginPercentage(totalFinalPrice, totalEstimatedCost);
    }
}
