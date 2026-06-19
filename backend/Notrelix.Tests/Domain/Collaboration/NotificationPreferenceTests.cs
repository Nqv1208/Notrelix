using FluentAssertions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration;

public class NotificationPreferenceTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var pref = NotificationPreference.Create(Guid.NewGuid(), NotificationChannel.Email);

        pref.Channel.Should().Be(NotificationChannel.Email);
        pref.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDisabled_ShouldSetEnabledFalse()
    {
        var pref = NotificationPreference.Create(Guid.NewGuid(), NotificationChannel.Push, enabled: false);

        pref.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Create_WithWorkspaceId_ShouldSetWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        var pref = NotificationPreference.Create(Guid.NewGuid(), NotificationChannel.InApp, workspaceId);

        pref.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => NotificationPreference.Create(Guid.Empty, NotificationChannel.Email);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SetEnabled_ShouldUpdate()
    {
        var pref = NotificationPreference.Create(Guid.NewGuid(), NotificationChannel.Push);

        pref.SetEnabled(false);

        pref.Enabled.Should().BeFalse();
    }
}
