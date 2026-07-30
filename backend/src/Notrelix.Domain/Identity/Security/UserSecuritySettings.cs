using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security.Events;

namespace Notrelix.Domain.Identity.Security;

public sealed class UserSecuritySettings : AggregateRoot
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
        settings.RaiseDomainEvent(new UserSecuritySettingsCreatedDomainEvent(settings.Id, userId, createdAt));
        return settings;
    }

    public void EnableMfa(MfaMethodType method, DateTimeOffset updatedAt)
    {
        if (IsMfaEnabled && PreferredMfaMethod == method) return;

        var pending = PrepareAuditUpdate(UserId, updatedAt);
        IsMfaEnabled = true;
        PreferredMfaMethod = method;
        LastSecurityReviewAt = updatedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserMfaRequirementEnabledDomainEvent(UserId, method, updatedAt));
    }

    public void DisableMfa(DateTimeOffset updatedAt)
    {
        if (!IsMfaEnabled) return;

        var previousMethod = PreferredMfaMethod;

        var pending = PrepareAuditUpdate(UserId, updatedAt);
        IsMfaEnabled = false;
        PreferredMfaMethod = null;
        LastSecurityReviewAt = updatedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserMfaRequirementDisabledDomainEvent(UserId, previousMethod, updatedAt));
    }

    public void RequirePasswordChangeNow(DateTimeOffset updatedAt)
    {
        var pending = PrepareAuditUpdate(UserId, updatedAt);
        RequirePasswordChange = true;
        LastSecurityReviewAt = updatedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new PasswordChangeRequiredDomainEvent(UserId, updatedAt));
    }

    public void MarkPasswordChanged(DateTimeOffset updatedAt)
    {
        var pending = PrepareAuditUpdate(UserId, updatedAt);
        RequirePasswordChange = false;
        PasswordChangedAt = updatedAt;
        LastSecurityReviewAt = updatedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserSecurityPasswordChangedDomainEvent(UserId, updatedAt));
    }

    public void UpdateSettings(JsonValue settings, DateTimeOffset updatedAt)
    {
        Guard.NotNull(settings);

        var pending = PrepareAuditUpdate(UserId, updatedAt);
        SettingsJson = settings;
        LastSecurityReviewAt = updatedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserSecuritySettingsUpdatedDomainEvent(UserId, updatedAt));
    }
}
