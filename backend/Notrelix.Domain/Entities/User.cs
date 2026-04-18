using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;
using Notrelix.Domain.ValueObjects;

namespace Notrelix.Domain.Entities;

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
    private readonly List<Session> _sessions = new();
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();
    public UserProfile? Profile { get; private set; }

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

    public Session CreateSession(TimeSpan expiration)
    {
        var session = Session.Create(Id, expiration);
        _sessions.Add(session);
        return session;
    }

    public void RevokeSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        session?.Revoke();
    }

    public void RevokeAllSessions()
    {
        foreach (var session in _sessions)
        {
            session.Revoke();
        }
    }
}
