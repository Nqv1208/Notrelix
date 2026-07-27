using Notrelix.Domain.Identity.Profiles.Events;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;
using Notrelix.Domain.Identity.Users.Events;

namespace Notrelix.Domain.Identity.Users;

public class User : SoftDeletableAggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string Name { get; private set; } = null!;
    public string? Avatar { get; private set; }

    public string? AvatarUrl => Avatar;
    public string PasswordHash { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset? EmailConfirmedAt { get; private set; }
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
            Status = UserStatus.Active,
            EmailConfirmed = false,
            EmailConfirmedAt = null
        };

        user.SetAuditOnCreate(null, createdAt);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email.Value, user.Name, createdAt));
        return user;
    }

    public void UpdateProfile(
        string name,
        string? avatar,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);
        Guard.NotEmpty(updatedBy);

        var trimmedName = name.Trim();
        var normalizedAvatar = avatar?.Trim();

        if (trimmedName == Name && normalizedAvatar == Avatar)
            return;

        Name = trimmedName;
        Avatar = normalizedAvatar;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, updatedBy, updatedAt));
    }

    public void UpdateEmail(
        string email,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        var emailValue = Email.Create(email);

        if (Email == emailValue) return;

        var oldEmail = Email;

        Email = emailValue;
        NormalizedEmail = NormalizeEmail(emailValue.Value);
        EmailConfirmed = false;
        EmailConfirmedAt = null;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserEmailChangedDomainEvent(
            Id,
            oldEmail,
            Email,
            updatedBy,
            updatedAt));
    }

    public void UpdatePassword(
        string passwordHash,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(passwordHash);
        Guard.NotEmpty(updatedBy);

        PasswordHash = passwordHash;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id, updatedBy, updatedAt));
    }

    public void RecordLogin(DateTimeOffset loggedInAt)
    {
        EnsureNotDeleted();

        if (LastLoginAt.HasValue && loggedInAt <= LastLoginAt.Value)
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_Login_TimeCannotMoveBackwards,
                "Login timestamp cannot move backwards.");

        LastLoginAt = loggedInAt;

        SetAuditOnUpdate(Id, loggedInAt);
        IncrementVersion();
        RaiseDomainEvent(new UserLoggedInDomainEvent(Id, loggedInAt));
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

        RaiseDomainEvent(new UserActivatedDomainEvent(
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

        RaiseDomainEvent(new UserDeactivatedDomainEvent(
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

        RaiseDomainEvent(new UserSuspendedDomainEvent(
            Id,
            previousStatus,
            suspendedBy,
            suspendedAt,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));
    }

    public void ConfirmEmail(Guid? confirmedBy, DateTimeOffset confirmedAt)
    {
        EnsureNotDeleted();

        if (EmailConfirmed)
            return;

        EmailConfirmed = true;
        EmailConfirmedAt = confirmedAt;

        if (Status == UserStatus.PendingVerification)
        {
            Status = UserStatus.Active;
        }

        SetAuditOnUpdate(confirmedBy, confirmedAt);
        IncrementVersion();

        RaiseDomainEvent(new UserEmailConfirmedDomainEvent(
            Id, Email.Value, confirmedBy, confirmedAt));
    }

    public void LinkOAuthAccount(
        OAuthProvider provider,
        string providerId,
        OAuthProfileSnapshot profileSnapshot,
        OAuthToken? token,
        Guid linkedBy,
        DateTimeOffset linkedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(providerId);
        Guard.NotNull(profileSnapshot);
        Guard.NotEmpty(linkedBy);

        if (profileSnapshot.Provider != provider)
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_User_OAuthProviderMismatch,
                $"Profile snapshot provider ({profileSnapshot.Provider}) does not match link provider ({provider}).");

        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing != null)
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_User_OAuthProviderAlreadyLinked,
                $"Provider {provider} is already linked. Use UpdateOAuthProfile or RotateOAuthToken instead.");

        var oauth = OAuthAccount.Create(Id, provider, providerId, profileSnapshot, token);
        _oauthAccounts.Add(oauth);

        SetAuditOnUpdate(linkedBy, linkedAt);
        IncrementVersion();
        RaiseDomainEvent(new OAuthAccountLinkedDomainEvent(Id, provider, providerId, linkedBy, linkedAt));
    }

    public void UpdateOAuthProfile(
        OAuthProvider provider,
        OAuthProfileSnapshot profileSnapshot,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(profileSnapshot);
        Guard.NotEmpty(updatedBy);

        if (profileSnapshot.Provider != provider)
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_User_OAuthProviderMismatch,
                $"Profile snapshot provider ({profileSnapshot.Provider}) does not match provider ({provider}).");

        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null)
            throw new BusinessRuleException(
                IdentityRuleCodes.Identity_User_NoOAuthAccountForProvider,
                $"No OAuth account linked for provider {provider}.");

        if (existing.ProfileSnapshot == profileSnapshot)
            return;

        existing.UpdateProfileSnapshot(profileSnapshot);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new OAuthProfileUpdatedDomainEvent(Id, provider, updatedBy, updatedAt));
    }

    public void UnlinkOAuthAccount(OAuthProvider provider, DateTimeOffset unlinkedAt)
    {
        EnsureNotDeleted();
        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null) return;

        _oauthAccounts.Remove(existing);
        SetAuditOnUpdate(Id, unlinkedAt);
        IncrementVersion();
        RaiseDomainEvent(new OAuthAccountUnlinkedDomainEvent(Id, provider, existing.ProviderId, unlinkedAt));
    }

    public void RotateOAuthToken(OAuthProvider provider, OAuthToken newToken, Guid rotatedBy, DateTimeOffset rotatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newToken);
        Guard.NotEmpty(rotatedBy);

        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_User_NoOAuthAccountForProvider, $"No OAuth account linked for provider {provider}.");
        }

        existing.UpdateToken(newToken);
        SetAuditOnUpdate(rotatedBy, rotatedAt);
        IncrementVersion();
        RaiseDomainEvent(new OAuthTokenReferenceRotatedDomainEvent(Id, provider, rotatedBy, rotatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserSoftDeletedDomainEvent(Id, deletedBy, deletedAt, reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new UserRestoredDomainEvent(Id, restoredBy, restoredAt));
    }
}
