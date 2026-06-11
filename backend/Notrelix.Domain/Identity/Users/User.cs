using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Identity.Users;

public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Avatar { get; private set; }

    public string? AvatarUrl => Avatar;
    public string PasswordHash { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public UserProfile? Profile { get; private set; }

    private readonly List<OAuthAccount> _oauthAccounts = new();
    public IReadOnlyCollection<OAuthAccount> OAuthAccounts => _oauthAccounts.AsReadOnly();

    private User() : base() { }

    public static User Create(string email, string name, string passwordHash, DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(passwordHash);

        var user = new User
        {
            Email = Email.Create(email),
            Name = name.Trim(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active
        };

        user.SetAuditOnCreate(Guid.Empty, createdAt);
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, email, createdAt));
        return user;
    }

    public void UpdateProfile(string name, string? avatar, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Name = name.Trim();
        Avatar = avatar?.Trim();
        SetAuditOnUpdate(Id, updatedAt);
    }

    public void UpdateEmail(string email, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Email = Email.Create(email);
        SetAuditOnUpdate(Id, updatedAt);
    }

    public void UpdatePassword(string passwordHash, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        SetAuditOnUpdate(Id, updatedAt);
    }

    public void RecordLogin(DateTimeOffset loggedInAt)
    {
        EnsureNotDeleted();
        LastLoginAt = loggedInAt;
        AddDomainEvent(new UserLoggedInEvent(Id, loggedInAt, loggedInAt));
    }

    public void Activate()
    {
        EnsureNotDeleted();
        Status = UserStatus.Active;
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        Status = UserStatus.Inactive;
    }

    public void Suspend()
    {
        EnsureNotDeleted();
        Status = UserStatus.Suspended;
    }
}
