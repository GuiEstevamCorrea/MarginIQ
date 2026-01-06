namespace Domain.Enums;

/// <summary>
/// Represents the type of action performed in an audit log
/// </summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Approved = 4,
    Rejected = 5,
    AutoApproved = 6,
    Activated = 7,
    Deactivated = 8,
    StatusChanged = 9,
    AccessGranted = 10,
    AccessDenied = 11,
    DataImport = 12,
    DataExport = 13,
    DataSync = 14,
    Other = 99
}
