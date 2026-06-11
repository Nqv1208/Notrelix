using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;
using Notrelix.Domain.Identity.Users.Events;

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

    public static User Create(
        string email,
        string name,
        string passwordHash,
        DateTimeOffset createdAt)
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

        user.SetAuditOnCreate(null, createdAt);
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, createdAt));
        return user;
    }

    public void UpdateProfile(
        string name,
        string? avatar,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        Name = name.Trim();
        Avatar = avatar?.Trim();
        
        SetAuditOnUpdate(Id, updatedAt);
    }

    public void UpdateEmail(
        string email,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        var oldEmail = Email;

        Email = Email.Create(email);
        
        SetAuditOnUpdate(Id, updatedAt);
        AddDomainEvent(new UserEmailChangedEvent(
            Id,
            oldEmail,
            Email,
            updatedAt));
    }

    public void UpdatePassword(
        string passwordHash,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;

        SetAuditOnUpdate(Id, updatedAt);
        AddDomainEvent(new UserPasswordChangedEvent(Id, updatedAt));
    }

    public void RecordLogin(DateTimeOffset loggedInAt)
    {
        EnsureNotDeleted();

        LastLoginAt = loggedInAt;

        AddDomainEvent(new UserLoggedInEvent(Id, loggedInAt));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(activatedBy);
        
        if (Status == UserStatus.Active) return;

        var previousStatus = Status;

        Status = UserStatus.Active;
        SetAuditOnUpdate(activatedBy, activatedAt);

        AddDomainEvent(new UserActivatedEvent(
            Id,
            previousStatus,
            activatedBy,
            activatedAt,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
    }

    public void Deactivate(
        Guid deactivatedBy,
        DateTimeOffset deactivatedAt,
        string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(deactivatedBy);

        if (Status == UserStatus.Inactive) return;

        var previousStatus = Status;

        Status = UserStatus.Inactive;
        SetAuditOnUpdate(deactivatedBy, deactivatedAt);

        AddDomainEvent(new UserDeactivatedEvent(
            Id,
            previousStatus,
            deactivatedBy,
            deactivatedAt,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
    }

    public void Suspend(
        Guid suspendedBy,
        DateTimeOffset suspendedAt,
        string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(suspendedBy);

        if (Status == UserStatus.Suspended) return;

        var previousStatus = Status;

        Status = UserStatus.Suspended;
        SetAuditOnUpdate(suspendedBy, suspendedAt);

        AddDomainEvent(new UserSuspendedEvent(
            Id,
            previousStatus,
            suspendedBy,
            suspendedAt,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
    }

    public void LinkOAuthAccount(
        OAuthProvider provider,
        string providerId,
        JsonValue rawProfile,
        OAuthToken? token,
        DateTimeOffset linkedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(providerId);
        Guard.NotNull(rawProfile);

        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing != null)
        {
            if (existing.ProviderId != providerId.Trim())
            {
                throw new BusinessRuleException($"Provider {provider} is already linked with a different account.");
            }
            if (token != null)
            {
                existing.UpdateToken(token);
            }
        }
        else
        {
            var oauth = OAuthAccount.Create(Id, provider, providerId, rawProfile, token);
            _oauthAccounts.Add(oauth);
        }

        SetAuditOnUpdate(Id, linkedAt);
        AddDomainEvent(new OAuthAccountLinkedEvent(Id, provider, providerId, linkedAt));
    }

    public void UnlinkOAuthAccount(OAuthProvider provider, DateTimeOffset unlinkedAt)
    {
        EnsureNotDeleted();
        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null) return;

        _oauthAccounts.Remove(existing);
        SetAuditOnUpdate(Id, unlinkedAt);
        AddDomainEvent(new OAuthAccountUnlinkedEvent(Id, provider, existing.ProviderId, unlinkedAt));
    }

    public void RotateOAuthToken(OAuthProvider provider, OAuthToken newToken, DateTimeOffset rotatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newToken);
        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null)
        {
            throw new BusinessRuleException($"No OAuth account linked for provider {provider}.");
        }

        existing.UpdateToken(newToken);
        SetAuditOnUpdate(Id, rotatedAt);
        AddDomainEvent(new OAuthTokenReferenceRotatedEvent(Id, provider, rotatedAt));
    }
}
