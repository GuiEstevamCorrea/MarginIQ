using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a User in the system.
/// Users belong to a Company (multi-tenant) and have specific roles (Salesperson, Manager, Admin).
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// User's email address (used for login and notifications)
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// User's role/profile in the system
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// The company (tenant) this user belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Current status of the user
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Password hash (for authentication)
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// Date and time when the user was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the user was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Date and time of the last login
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    // Navigation property
    public Company? Company { get; private set; }

    // Private constructor for EF Core
    private User() { }

    /// <summary>
    /// Creates a new User instance
    /// </summary>
    /// <param name="name">User's full name</param>
    /// <param name="email">User's email address</param>
    /// <param name="role">User's role in the system</param>
    /// <param name="companyId">The company this user belongs to</param>
    /// <param name="passwordHash">Optional password hash</param>
    public User(string name, string email, UserRole role, Guid companyId, string? passwordHash = null)
    {
        ValidateName(name);
        ValidateEmail(email);

        Id = Guid.NewGuid();
        Name = name;
        Email = email.ToLowerInvariant();
        Role = role;
        CompanyId = companyId;
        Status = UserStatus.PendingActivation; // New users need activation
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user account
    /// </summary>
    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new InvalidOperationException("User is already active");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account
    /// </summary>
    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
            throw new InvalidOperationException("User is already inactive");

        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Blocks the user account
    /// </summary>
    public void Block()
    {
        if (Status == UserStatus.Blocked)
            throw new InvalidOperationException("User is already blocked");

        Status = UserStatus.Blocked;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unblocks the user account
    /// </summary>
    public void Unblock()
    {
        if (Status != UserStatus.Blocked)
            throw new InvalidOperationException("User is not blocked");

        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's email
    /// </summary>
    /// <param name="email">New email address</param>
    public void UpdateEmail(string email)
    {
        ValidateEmail(email);
        Email = email.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's role
    /// </summary>
    /// <param name="role">New role</param>
    public void UpdateRole(UserRole role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's password hash
    /// </summary>
    /// <param name="passwordHash">New password hash</param>
    public void UpdatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a user login
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the user is active and can perform operations
    /// </summary>
    /// <returns>True if the user is active, false otherwise</returns>
    public bool IsActive() => Status == UserStatus.Active;

    /// <summary>
    /// Checks if the user is blocked
    /// </summary>
    /// <returns>True if the user is blocked, false otherwise</returns>
    public bool IsBlocked() => Status == UserStatus.Blocked;

    /// <summary>
    /// Checks if the user is a salesperson
    /// </summary>
    /// <returns>True if the user is a salesperson, false otherwise</returns>
    public bool IsSalesperson() => Role == UserRole.Salesperson;

    /// <summary>
    /// Checks if the user is a manager
    /// </summary>
    /// <returns>True if the user is a manager, false otherwise</returns>
    public bool IsManager() => Role == UserRole.Manager;

    /// <summary>
    /// Checks if the user is an administrator
    /// </summary>
    /// <returns>True if the user is an administrator, false otherwise</returns>
    public bool IsAdmin() => Role == UserRole.Admin;

    /// <summary>
    /// Checks if the user can approve discount requests
    /// </summary>
    /// <returns>True if the user can approve (Manager or Admin), false otherwise</returns>
    public bool CanApproveDiscounts() => Role == UserRole.Manager || Role == UserRole.Admin;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name cannot be empty", nameof(name));

        if (name.Length < 2)
            throw new ArgumentException("User name must have at least 2 characters", nameof(name));

        if (name.Length > 150)
            throw new ArgumentException("User name cannot exceed 150 characters", nameof(name));
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!email.Contains('@') || !email.Contains('.'))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (email.Length > 254)
            throw new ArgumentException("Email cannot exceed 254 characters", nameof(email));
    }
}
