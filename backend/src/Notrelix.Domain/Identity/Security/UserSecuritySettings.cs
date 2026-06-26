using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security.Events;

namespace Notrelix.Domain.Identity.Security;

public class UserSecuritySettings : AggregateRoot
{
    public Guid UserId { get; private set; }
    public bool IsMfaEnabled { get; private set; }
    public MfaMethodType? PreferredMfaMethod { get; private set; }
    public bool RequirePasswordChange { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }
    public DateTimeOffset? LastSecurityReviewAt { get; private set; }
    public JsonValue SettingsJson { get; private set; } = null!;

    private UserSecuritySettings() : base() { }

    public static UserSecuritySettings Create(Guid userId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(userId);
        var settings = new UserSecuritySettings
        {
            UserId = userId,
            IsMfaEnabled = false,
            SettingsJson = JsonValue.Create("{}")
        };
        settings.SetAuditOnCreate(userId, createdAt);
        settings.AddDomainEvent(new UserSecuritySettingsCreatedDomainEvent(settings.Id, userId, createdAt));
        return settings;
    }

    public void EnableMfa(MfaMethodType method, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (IsMfaEnabled && PreferredMfaMethod == method) return;

        IsMfaEnabled = true;
        PreferredMfaMethod = method;
        LastSecurityReviewAt = updatedAt;

        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserMfaRequirementEnabledDomainEvent(UserId, method, updatedAt));
    }

    public void DisableMfa(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        if (!IsMfaEnabled) return;

        var previousMethod = PreferredMfaMethod;

        IsMfaEnabled = false;
        PreferredMfaMethod = null;
        LastSecurityReviewAt = updatedAt;

        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserMfaRequirementDisabledDomainEvent(UserId, previousMethod, updatedAt));
    }

    public void RequirePasswordChangeNow(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        RequirePasswordChange = true;
        LastSecurityReviewAt = updatedAt;

        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new PasswordChangeRequiredDomainEvent(UserId, updatedAt));
    }

    public void MarkPasswordChanged(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();

        RequirePasswordChange = false;
        PasswordChangedAt = updatedAt;
        LastSecurityReviewAt = updatedAt;

        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserSecurityPasswordChangedDomainEvent(UserId, updatedAt));
    }

    public void UpdateSettings(JsonValue settings, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(settings);

        SettingsJson = settings;
        LastSecurityReviewAt = updatedAt;

        SetAuditOnUpdate(UserId, updatedAt);
        AddDomainEvent(new UserSecuritySettingsUpdatedDomainEvent(UserId, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new UserSecuritySettingsSoftDeletedDomainEvent(Id, UserId, deletedBy, deletedAt, reason));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new UserSecuritySettingsRestoredDomainEvent(Id, UserId, restoredBy, restoredAt));
    }
}