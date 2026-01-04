namespace Domain.Services;

/// <summary>
/// Represents the result of a business rule validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates if the validation passed
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// List of validation error messages
    /// </summary>
    public List<string> Errors { get; private set; }

    /// <summary>
    /// List of validation warning messages
    /// </summary>
    public List<string> Warnings { get; private set; }

    public ValidationResult()
    {
        IsValid = true;
        Errors = new List<string>();
        Warnings = new List<string>();
    }

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="error">Error message</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
            IsValid = false;
        }
    }

    /// <summary>
    /// Adds a warning to the validation result
    /// </summary>
    /// <param name="warning">Warning message</param>
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// Adds multiple errors
    /// </summary>
    public void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            AddError(error);
        }
    }

    /// <summary>
    /// Merges another validation result into this one
    /// </summary>
    public void Merge(ValidationResult other)
    {
        if (other == null) return;

        AddErrors(other.Errors);
        foreach (var warning in other.Warnings)
        {
            AddWarning(warning);
        }
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        var result = new ValidationResult();
        result.AddErrors(errors);
        return result;
    }
}
