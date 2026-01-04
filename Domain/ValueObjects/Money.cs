namespace Domain.ValueObjects;

/// <summary>
/// Value object representing a monetary amount
/// Ensures precision and consistency in financial calculations
/// </summary>
public class Money : IEquatable<Money>
{
    /// <summary>
    /// The monetary value
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>
    /// Currency code (e.g., USD, BRL, EUR)
    /// </summary>
    public string Currency { get; private set; }

    // Private constructor for EF Core
    private Money() 
    {
        Currency = "USD";
    }

    /// <summary>
    /// Creates a new Money instance
    /// </summary>
    /// <param name="value">The monetary value</param>
    /// <param name="currency">Currency code (defaults to USD)</param>
    public Money(decimal value, string currency = "USD")
    {
        if (value < 0)
            throw new ArgumentException("Money value cannot be negative", nameof(value));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter code (e.g., USD, BRL, EUR)", nameof(currency));

        Value = Math.Round(value, 2); // Always round to 2 decimal places
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Creates a Money instance with zero value
    /// </summary>
    /// <param name="currency">Currency code (defaults to USD)</param>
    /// <returns>Money instance with zero value</returns>
    public static Money Zero(string currency = "USD") => new Money(0, currency);

    /// <summary>
    /// Adds two Money instances (must have same currency)
    /// </summary>
    public static Money operator +(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return new Money(left.Value + right.Value, left.Currency);
    }

    /// <summary>
    /// Subtracts two Money instances (must have same currency)
    /// </summary>
    public static Money operator -(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return new Money(left.Value - right.Value, left.Currency);
    }

    /// <summary>
    /// Multiplies Money by a decimal value
    /// </summary>
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Value * multiplier, money.Currency);
    }

    /// <summary>
    /// Divides Money by a decimal value
    /// </summary>
    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");

        return new Money(money.Value / divisor, money.Currency);
    }

    /// <summary>
    /// Checks if left is greater than right
    /// </summary>
    public static bool operator >(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Value > right.Value;
    }

    /// <summary>
    /// Checks if left is less than right
    /// </summary>
    public static bool operator <(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Value < right.Value;
    }

    /// <summary>
    /// Checks if left is greater than or equal to right
    /// </summary>
    public static bool operator >=(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Value >= right.Value;
    }

    /// <summary>
    /// Checks if left is less than or equal to right
    /// </summary>
    public static bool operator <=(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Value <= right.Value;
    }

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Value == other.Value && Currency == other.Currency;
    }

    public override bool Equals(object? obj)
    {
        return obj is Money money && Equals(money);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Currency);
    }

    public static bool operator ==(Money? left, Money? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Currency} {Value:N2}";
    }

    private static void ValidateSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot operate on different currencies: {left.Currency} and {right.Currency}");
    }
}
