namespace Domain.Enums;

/// <summary>
/// Represents the classification/tier of a customer (optional)
/// Used for segmentation and differentiated discount policies
/// </summary>
public enum CustomerClassification
{
    /// <summary>
    /// A-tier customer - highest priority, largest volume or strategic importance
    /// </summary>
    A = 1,
    
    /// <summary>
    /// B-tier customer - medium priority and volume
    /// </summary>
    B = 2,
    
    /// <summary>
    /// C-tier customer - lower priority or smaller volume
    /// </summary>
    C = 3,
    
    /// <summary>
    /// Unclassified - customer has not been classified yet
    /// </summary>
    Unclassified = 0
}
