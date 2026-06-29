using Notrelix.Domain.Identity.Profiles.Events;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;
using Notrelix.Domain.Identity.Users.Events;

namespace Notrelix.Domain.Identity.Users;

public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string Name { get; private set; } = null!;
    public string? Avatar { get; private set; }

    public string? AvatarUrl => Avatar;
    public string PasswordHash { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private readonly List<OAuthAccount> _oauthAccounts = new();
    public IReadOnlyCollection<OAuthAccount> OAuthAccounts => _oauthAccounts.AsReadOnly();

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private User() : base() { }

    public static User Create(
        string email,
        string name,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.NotNullOrWhiteSpace(passwordHash);

        var emailValue = Email.Create(email);

        var user = new User
        {
            Email = emailValue,
            NormalizedEmail = NormalizeEmail(emailValue.Value),
            Name = name.Trim(),
            PasswordHash = passwordHash,
            Status = UserStatus.Active
        };

        user.SetAuditOnCreate(null, createdAt);
        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, createdAt));
        return user;
    }

    public void UpdateProfile(
        string name,
        string? avatar,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);

        Name = name.Trim();
        Avatar = avatar?.Trim();

        SetAuditOnUpdate(Id, updatedAt);
        IncrementVersion();
        AddDomainEvent(new UserProfileUpdatedDomainEvent(Id, updatedAt));
    }

    public void UpdateEmail(
        string email,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        var emailValue = Email.Create(email);

        if (Email == emailValue) return;

        var oldEmail = Email;

        Email = emailValue;
        NormalizedEmail = NormalizeEmail(emailValue.Value);

        SetAuditOnUpdate(Id, updatedAt);
        IncrementVersion();
        AddDomainEvent(new UserEmailChangedDomainEvent(
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
        IncrementVersion();
        AddDomainEvent(new UserPasswordChangedDomainEvent(Id, updatedAt));
    }

    public void RecordLogin(DateTimeOffset loggedInAt)
    {
        EnsureNotDeleted();

        LastLoginAt = loggedInAt;

        SetAuditOnUpdate(Id, loggedInAt);
        IncrementVersion();
        AddDomainEvent(new UserLoggedInDomainEvent(Id, loggedInAt));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(activatedBy);

        if (Status == UserStatus.Active) return;

        var previousStatus = Status;

        Status = UserStatus.Active;
        SetAuditOnUpdate(activatedBy, activatedAt);
        IncrementVersion();

        AddDomainEvent(new UserActivatedDomainEvent(
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
        IncrementVersion();

        AddDomainEvent(new UserDeactivatedDomainEvent(
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
        IncrementVersion();

        AddDomainEvent(new UserSuspendedDomainEvent(
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
        IncrementVersion();
        AddDomainEvent(new OAuthAccountLinkedDomainEvent(Id, provider, providerId, linkedAt));
    }

    public void UnlinkOAuthAccount(OAuthProvider provider, DateTimeOffset unlinkedAt)
    {
        EnsureNotDeleted();
        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null) return;

        _oauthAccounts.Remove(existing);
        SetAuditOnUpdate(Id, unlinkedAt);
        IncrementVersion();
        AddDomainEvent(new OAuthAccountUnlinkedDomainEvent(Id, provider, existing.ProviderId, unlinkedAt));
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
        IncrementVersion();
        AddDomainEvent(new OAuthTokenReferenceRotatedDomainEvent(Id, provider, rotatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new UserSoftDeletedDomainEvent(Id, deletedBy, deletedAt, reason));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new UserRestoredDomainEvent(Id, restoredBy, restoredAt));
    }
}
