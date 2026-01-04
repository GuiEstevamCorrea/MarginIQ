using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents an Audit Log entry for tracking all actions in the system.
/// Provides complete traceability of who did what, when, and from which origin (Human, AI, System).
/// Immutable by design - audit logs should never be modified after creation.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Name of the entity that was acted upon (e.g., "DiscountRequest", "User", "Product")
    /// </summary>
    public string EntityName { get; private set; }

    /// <summary>
    /// ID of the entity that was acted upon
    /// </summary>
    public Guid EntityId { get; private set; }

    /// <summary>
    /// Action performed (Created, Updated, Deleted, Approved, etc.)
    /// </summary>
    public AuditAction Action { get; private set; }

    /// <summary>
    /// Origin/source of the action (Human, AI, System)
    /// </summary>
    public AuditOrigin Origin { get; private set; }

    /// <summary>
    /// User ID who performed the action (null if AI or System)
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Payload with details of the action (e.g., old values, new values, parameters)
    /// Stored as JSON for flexibility
    /// </summary>
    public string? Payload { get; private set; }

    /// <summary>
    /// Date and time when the action occurred
    /// </summary>
    public DateTime DateTime { get; private set; }

    /// <summary>
    /// Company ID (tenant) for multi-tenant isolation
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// IP address or source identifier
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent or additional context
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Additional metadata stored as JSON
    /// </summary>
    public string? Metadata { get; private set; }

    // Navigation properties
    public Company? Company { get; private set; }
    public User? User { get; private set; }

    // Private constructor for EF Core
    private AuditLog() 
    {
        EntityName = string.Empty;
    }

    /// <summary>
    /// Creates a new AuditLog entry
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="action">Action performed</param>
    /// <param name="origin">Origin of the action</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="userId">Optional user ID (for human actions)</param>
    /// <param name="payload">Optional payload JSON</param>
    /// <param name="ipAddress">Optional IP address</param>
    /// <param name="userAgent">Optional user agent</param>
    /// <param name="metadata">Optional metadata JSON</param>
    public AuditLog(
        string entityName,
        Guid entityId,
        AuditAction action,
        AuditOrigin origin,
        Guid companyId,
        Guid? userId = null,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null)
    {
        ValidateEntityName(entityName);

        Id = Guid.NewGuid();
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        Origin = origin;
        CompanyId = companyId;
        UserId = userId;
        Payload = payload;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Metadata = metadata;
        DateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an audit log for a human action
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="action">Action performed</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="userId">User ID who performed the action</param>
    /// <param name="payload">Optional payload JSON</param>
    /// <param name="ipAddress">Optional IP address</param>
    /// <param name="userAgent">Optional user agent</param>
    /// <returns>AuditLog instance</returns>
    public static AuditLog CreateForHuman(
        string entityName,
        Guid entityId,
        AuditAction action,
        Guid companyId,
        Guid userId,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog(
            entityName,
            entityId,
            action,
            AuditOrigin.Human,
            companyId,
            userId,
            payload,
            ipAddress,
            userAgent);
    }

    /// <summary>
    /// Creates an audit log for an AI action
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="action">Action performed</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="payload">Optional payload JSON with AI details</param>
    /// <param name="metadata">Optional metadata JSON (e.g., model version, confidence)</param>
    /// <returns>AuditLog instance</returns>
    public static AuditLog CreateForAI(
        string entityName,
        Guid entityId,
        AuditAction action,
        Guid companyId,
        string? payload = null,
        string? metadata = null)
    {
        return new AuditLog(
            entityName,
            entityId,
            action,
            AuditOrigin.AI,
            companyId,
            null, // AI has no user ID
            payload,
            null,
            null,
            metadata);
    }

    /// <summary>
    /// Creates an audit log for a system action
    /// </summary>
    /// <param name="entityName">Name of the entity</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="action">Action performed</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="payload">Optional payload JSON</param>
    /// <param name="metadata">Optional metadata JSON</param>
    /// <returns>AuditLog instance</returns>
    public static AuditLog CreateForSystem(
        string entityName,
        Guid entityId,
        AuditAction action,
        Guid companyId,
        string? payload = null,
        string? metadata = null)
    {
        return new AuditLog(
            entityName,
            entityId,
            action,
            AuditOrigin.System,
            companyId,
            null, // System has no user ID
            payload,
            null,
            null,
            metadata);
    }

    /// <summary>
    /// Checks if this audit log entry was created by a human
    /// </summary>
    /// <returns>True if created by human, false otherwise</returns>
    public bool IsHumanAction() => Origin == AuditOrigin.Human;

    /// <summary>
    /// Checks if this audit log entry was created by AI
    /// </summary>
    /// <returns>True if created by AI, false otherwise</returns>
    public bool IsAIAction() => Origin == AuditOrigin.AI;

    /// <summary>
    /// Checks if this audit log entry was created by system
    /// </summary>
    /// <returns>True if created by system, false otherwise</returns>
    public bool IsSystemAction() => Origin == AuditOrigin.System;

    /// <summary>
    /// Gets the actor identifier (User ID, "AI", or "System")
    /// </summary>
    /// <returns>Actor identifier as string</returns>
    public string GetActorIdentifier()
    {
        if (UserId.HasValue)
            return UserId.Value.ToString();

        return Origin switch
        {
            AuditOrigin.AI => "AI",
            AuditOrigin.System => "System",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets a human-readable description of the action
    /// </summary>
    /// <returns>Action description</returns>
    public string GetActionDescription()
    {
        var actorType = Origin switch
        {
            AuditOrigin.Human => "User",
            AuditOrigin.AI => "AI",
            AuditOrigin.System => "System",
            _ => "Unknown"
        };

        var actionText = Action switch
        {
            AuditAction.Created => "created",
            AuditAction.Updated => "updated",
            AuditAction.Deleted => "deleted",
            AuditAction.Approved => "approved",
            AuditAction.Rejected => "rejected",
            AuditAction.AutoApproved => "auto-approved",
            AuditAction.Activated => "activated",
            AuditAction.Deactivated => "deactivated",
            AuditAction.StatusChanged => "changed status of",
            AuditAction.AccessGranted => "granted access to",
            AuditAction.AccessDenied => "denied access to",
            _ => "performed action on"
        };

        return $"{actorType} {actionText} {EntityName} ({EntityId})";
    }

    private static void ValidateEntityName(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be empty", nameof(entityName));

        if (entityName.Length > 100)
            throw new ArgumentException("Entity name cannot exceed 100 characters", nameof(entityName));
    }
}
