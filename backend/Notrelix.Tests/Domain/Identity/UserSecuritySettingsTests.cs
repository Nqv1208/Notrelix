using FluentAssertions;
using Notrelix.Domain.Identity.Security;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class UserSecuritySettingsTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var userId = Guid.NewGuid();

        var settings = UserSecuritySettings.Create(userId);

        settings.UserId.Should().Be(userId);
        settings.IsMfaEnabled.Should().BeFalse();
        settings.RequirePasswordChange.Should().BeFalse();
    }

    [Fact]
    public void EnableMfa_ShouldSetMethodAndRaiseEvent()
    {
        var settings = UserSecuritySettings.Create(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        settings.EnableMfa(MfaMethodType.AuthenticatorApp, now);

        settings.IsMfaEnabled.Should().BeTrue();
        settings.PreferredMfaMethod.Should().Be(MfaMethodType.AuthenticatorApp);
        settings.DomainEvents.Should().ContainSingle(e => e is MfaEnabledEvent);
    }

    [Fact]
    public void DisableMfa_ShouldClearMethodAndRaiseEvent()
    {
        var settings = UserSecuritySettings.Create(Guid.NewGuid());
        settings.EnableMfa(MfaMethodType.AuthenticatorApp, DateTimeOffset.UtcNow);
        settings.ClearDomainEvents();

        settings.DisableMfa(DateTimeOffset.UtcNow);

        settings.IsMfaEnabled.Should().BeFalse();
        settings.PreferredMfaMethod.Should().BeNull();
        settings.DomainEvents.Should().ContainSingle(e => e is MfaDisabledEvent);
    }

    [Fact]
    public void RequirePasswordChangeNow_ShouldSetFlag()
    {
        var settings = UserSecuritySettings.Create(Guid.NewGuid());

        settings.RequirePasswordChangeNow(DateTimeOffset.UtcNow);

        settings.RequirePasswordChange.Should().BeTrue();
    }

    [Fact]
    public void PasswordChanged_ShouldClearFlag()
    {
        var settings = UserSecuritySettings.Create(Guid.NewGuid());
        settings.RequirePasswordChangeNow(DateTimeOffset.UtcNow);

        settings.PasswordChanged(DateTimeOffset.UtcNow);

        settings.RequirePasswordChange.Should().BeFalse();
    }
}
