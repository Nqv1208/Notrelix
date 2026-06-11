namespace Notrelix.Domain.Identity;

// Entity đại diện cho người dùng trong hệ thống
public class User : AuditableEntity
{
    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Avatar { get; private set; }
    
    // Computed property for compatibility
    public string? AvatarUrl => Avatar;
    public string PasswordHash { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Navigation - Sessions
    private readonly List<UserSession> _sessions = new();
    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();
    public UserProfile? Profile { get; private set; }

    // Navigation - OAuth Accounts
    private readonly List<OAuthAccount> _oauthAccounts = new();
    public IReadOnlyCollection<OAuthAccount> OAuthAccounts => _oauthAccounts.AsReadOnly();

    private User() : base() { }

    public static User Create(string email, string name, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống", nameof(name));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash không được để trống", nameof(passwordHash));

        return new User
        {
            Email = Email.Create(email),
            Name = name.Trim(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active
        };
    }

    public void UpdateProfile(string name, string? avatar)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống", nameof(name));

        Name = name.Trim();
        Avatar = avatar?.Trim();
    }

    public void UpdateEmail(string email)
    {
        Email = Email.Create(email);
    }

    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash không được để trống", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Activate() => Status = UserStatus.Active;
    public void Deactivate() => Status = UserStatus.Inactive;
    public void Suspend() => Status = UserStatus.Suspended;

    public UserSession CreateSession(RefreshTokenHash tokenHash, DateTimeOffset expiration, DateTimeOffset createdAt, string? ipAddress = null, string? userAgent = null)
    {
        var session = UserSession.Create(Id, tokenHash, expiration, createdAt, ipAddress, userAgent);
        _sessions.Add(session);
        return session;
    }

    public void RevokeSession(Guid sessionId, DateTimeOffset revokedAt)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        session?.Revoke(revokedAt);
    }

    public void RevokeAllSessions(DateTimeOffset revokedAt)
    {
        foreach (var session in _sessions)
        {
            session.Revoke(revokedAt);
        }
    }
}
