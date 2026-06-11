using FluentAssertions;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Security.Events;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class UserSecuritySettingsTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var settings = UserSecuritySettings.Create(userId, now);

        settings.UserId.Should().Be(userId);
        settings.IsMfaEnabled.Should().BeFalse();
        settings.RequirePasswordChange.Should().BeFalse();
        settings.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void EnableMfa_ShouldSetMethodAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var settings = UserSecuritySettings.Create(Guid.NewGuid(), now);

        settings.EnableMfa(MfaMethodType.AuthenticatorApp, now);

        settings.IsMfaEnabled.Should().BeTrue();
        settings.PreferredMfaMethod.Should().Be(MfaMethodType.AuthenticatorApp);
        settings.DomainEvents.Should().ContainSingle(e => e is UserMfaRequirementEnabledEvent);
        var evt = (UserMfaRequirementEnabledEvent)settings.DomainEvents.Single(e => e is UserMfaRequirementEnabledEvent);
        evt.UserId.Should().Be(settings.UserId);
        evt.PreferredMethod.Should().Be(MfaMethodType.AuthenticatorApp);
        evt.EnabledAt.Should().Be(now);
    }

    [Fact]
    public void DisableMfa_ShouldClearMethodAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var settings = UserSecuritySettings.Create(Guid.NewGuid(), now);
        settings.EnableMfa(MfaMethodType.AuthenticatorApp, now);
        settings.ClearDomainEvents();

        settings.DisableMfa(now.AddMinutes(1));

        settings.IsMfaEnabled.Should().BeFalse();
        settings.PreferredMfaMethod.Should().BeNull();
        settings.DomainEvents.Should().ContainSingle(e => e is UserMfaRequirementDisabledEvent);
        var evt = (UserMfaRequirementDisabledEvent)settings.DomainEvents.Single(e => e is UserMfaRequirementDisabledEvent);
        evt.UserId.Should().Be(settings.UserId);
        evt.PreviousMethod.Should().Be(MfaMethodType.AuthenticatorApp);
        evt.DisabledAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void RequirePasswordChangeNow_ShouldSetFlagAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var settings = UserSecuritySettings.Create(Guid.NewGuid(), now);

        settings.RequirePasswordChangeNow(now);

        settings.RequirePasswordChange.Should().BeTrue();
        settings.DomainEvents.Should().ContainSingle(e => e is PasswordChangeRequiredEvent);
        var evt = (PasswordChangeRequiredEvent)settings.DomainEvents.Single(e => e is PasswordChangeRequiredEvent);
        evt.UserId.Should().Be(settings.UserId);
        evt.RequiredAt.Should().Be(now);
    }

    [Fact]
    public void MarkPasswordChanged_ShouldClearFlagAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var settings = UserSecuritySettings.Create(Guid.NewGuid(), now);
        settings.RequirePasswordChangeNow(now);
        settings.ClearDomainEvents();

        settings.MarkPasswordChanged(now.AddMinutes(1));

        settings.RequirePasswordChange.Should().BeFalse();
        settings.DomainEvents.Should().ContainSingle(e => e is UserSecurityPasswordChangedEvent);
        var evt = (UserSecurityPasswordChangedEvent)settings.DomainEvents.Single(e => e is UserSecurityPasswordChangedEvent);
        evt.UserId.Should().Be(settings.UserId);
        evt.ChangedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void UpdateSettings_ShouldSetSettingsJsonAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var settings = UserSecuritySettings.Create(Guid.NewGuid(), now);
        var settingsJson = JsonValue.Create("{\"theme\":\"dark\"}");

        settings.UpdateSettings(settingsJson, now);

        settings.SettingsJson.Value.Should().Be("{\"theme\":\"dark\"}");
        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsUpdatedEvent);
        var evt = (UserSecuritySettingsUpdatedEvent)settings.DomainEvents.Single(e => e is UserSecuritySettingsUpdatedEvent);
        evt.UserId.Should().Be(settings.UserId);
        evt.UpdatedAt.Should().Be(now);
    }
}
