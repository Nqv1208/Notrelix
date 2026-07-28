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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 100);

        var trimmedName = name.Trim();
        var normalizedAvatar = avatar?.Trim();

        if (trimmedName == Name && normalizedAvatar == Avatar)
            return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = trimmedName;
        Avatar = normalizedAvatar;
        ApplyAuditUpdate(pending);
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

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Email = emailValue;
        NormalizedEmail = NormalizeEmail(emailValue.Value);
        EmailConfirmed = false;
        EmailConfirmedAt = null;
        ApplyAuditUpdate(pending);
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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(passwordHash);

        if (string.Equals(PasswordHash, passwordHash, StringComparison.Ordinal))
            return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        PasswordHash = passwordHash;
        ApplyAuditUpdate(pending);
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

        var pending = PrepareAuditUpdate(Id, loggedInAt);
        LastLoginAt = loggedInAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserLoggedInDomainEvent(Id, loggedInAt));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(activatedBy);

        if (Status == UserStatus.Active) return;

        var previousStatus = Status;
        var pending = PrepareAuditUpdate(activatedBy, activatedAt);
        Status = UserStatus.Active;
        ApplyAuditUpdate(pending);
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
        var pending = PrepareAuditUpdate(deactivatedBy, deactivatedAt);
        Status = UserStatus.Inactive;
        ApplyAuditUpdate(pending);
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
        var pending = PrepareAuditUpdate(suspendedBy, suspendedAt);
        Status = UserStatus.Suspended;
        ApplyAuditUpdate(pending);
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

        var pending = PrepareAuditUpdate(confirmedBy, confirmedAt);
        EmailConfirmed = true;
        EmailConfirmedAt = confirmedAt;

        if (Status == UserStatus.PendingVerification)
        {
            Status = UserStatus.Active;
        }
        ApplyAuditUpdate(pending);
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
        Guard.NotEmpty(linkedBy);
        Guard.NotNullOrWhiteSpace(providerId);
        Guard.NotNull(profileSnapshot);

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

        var pending = PrepareAuditUpdate(linkedBy, linkedAt);
        ApplyAuditUpdate(pending);
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
        Guard.NotEmpty(updatedBy);
        Guard.NotNull(profileSnapshot);

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
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new OAuthProfileUpdatedDomainEvent(Id, provider, updatedBy, updatedAt));
    }

    public void UnlinkOAuthAccount(OAuthProvider provider, Guid unlinkedBy, DateTimeOffset unlinkedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(unlinkedBy);

        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null) return;

        _oauthAccounts.Remove(existing);
        var pending = PrepareAuditUpdate(unlinkedBy, unlinkedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new OAuthAccountUnlinkedDomainEvent(Id, provider, existing.ProviderId, unlinkedBy, unlinkedAt));
    }

    public void RotateOAuthToken(OAuthProvider provider, OAuthToken newToken, Guid rotatedBy, DateTimeOffset rotatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(rotatedBy);
        Guard.NotNull(newToken);

        var existing = _oauthAccounts.FirstOrDefault(x => x.Provider == provider);
        if (existing == null)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_User_NoOAuthAccountForProvider, $"No OAuth account linked for provider {provider}.");
        }

        if (existing.Token == newToken) return;

        existing.UpdateToken(newToken);
        var pending = PrepareAuditUpdate(rotatedBy, rotatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new OAuthTokenReferenceRotatedDomainEvent(Id, provider, rotatedBy, rotatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new UserSoftDeletedDomainEvent(Id, deletedBy, deletedAt, reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new UserRestoredDomainEvent(Id, restoredBy, restoredAt));
    }
}
