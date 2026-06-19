using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Profiles.Events;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Users.Events;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.OAuth.Events;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Teams.Events;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Spaces.Events;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Executions.Events;
using Notrelix.Domain.Automation.Agents;
using Notrelix.Domain.Automation.Agents.Events;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Collaboration.Notifications.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests;

public class Phase2_1AuditTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    #region User

    [Fact]
    public void User_UpdateProfile_ShouldIncrementVersion_AndEmitEvent()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.UpdateProfile("New Name", null, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void User_UpdateEmail_ShouldIncrementVersion()
    {
        var user = User.Create("old@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.UpdateEmail("new@test.com", _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserEmailChangedDomainEvent);
    }

    [Fact]
    public void User_UpdatePassword_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.UpdatePassword("newhash", _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserPasswordChangedDomainEvent);
    }

    [Fact]
    public void User_RecordLogin_ShouldIncrementVersion_AndSetAudit()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.RecordLogin(_now);

        user.Version.Should().Be(version + 1);
        user.LastLoginAt.Should().Be(_now);
        user.DomainEvents.Should().Contain(e => e is UserLoggedInDomainEvent);
    }

    [Fact]
    public void User_Activate_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        user.Deactivate(_actorId, _now);
        var version = user.Version;

        user.Activate(_actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserActivatedDomainEvent);
    }

    [Fact]
    public void User_Deactivate_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.Deactivate(_actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserDeactivatedDomainEvent);
    }

    [Fact]
    public void User_Suspend_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.Suspend(_actorId, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is UserSuspendedDomainEvent);
    }

    [Fact]
    public void User_LinkOAuthAccount_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var version = user.Version;

        user.LinkOAuthAccount(OAuthProvider.Google, "pid123", JsonValue.Null(), null, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is OAuthAccountLinkedDomainEvent);
    }

    [Fact]
    public void User_UnlinkOAuthAccount_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123", JsonValue.Null(), null, _now);
        user.ClearDomainEvents();
        var version = user.Version;

        user.UnlinkOAuthAccount(OAuthProvider.Google, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is OAuthAccountUnlinkedDomainEvent);
    }

    [Fact]
    public void User_RotateOAuthToken_ShouldIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        var token = OAuthToken.Create(SecretRef.Create("access"), SecretRef.Create("refresh"), _now.AddHours(1));
        user.LinkOAuthAccount(OAuthProvider.Google, "pid123", JsonValue.Null(), token, _now);
        user.ClearDomainEvents();
        var version = user.Version;
        var newToken = OAuthToken.Create(SecretRef.Create("new-access"), SecretRef.Create("new-refresh"), _now.AddHours(2));

        user.RotateOAuthToken(OAuthProvider.Google, newToken, _now);

        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().Contain(e => e is OAuthTokenReferenceRotatedDomainEvent);
    }

    [Fact]
    public void User_Activate_WhenAlreadyActive_ShouldNotIncrementVersion()
    {
        var user = User.Create("test@test.com", "Test", "hash", _now);
        user.ClearDomainEvents();
        var version = user.Version;

        user.Activate(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region WorkspaceMember

    [Fact]
    public void WorkspaceMember_ChangeRole_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.ClearDomainEvents();
        var version = member.Version;

        member.ChangeRole(WorkspaceRole.Admin, _actorId, 2, _now);

        member.Version.Should().Be(version + 1);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRoleChangedDomainEvent);
    }

    [Fact]
    public void WorkspaceMember_Suspend_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.ClearDomainEvents();
        var version = member.Version;

        member.Suspend(_actorId, _now, 2);

        member.Version.Should().Be(version + 1);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberSuspendedDomainEvent);
    }

    [Fact]
    public void WorkspaceMember_Activate_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.Suspend(_actorId, _now, 2);
        member.ClearDomainEvents();
        var version = member.Version;

        member.Activate(_actorId, _now);

        member.Version.Should().Be(version + 1);
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberActivatedDomainEvent);
    }

    [Fact]
    public void WorkspaceMember_SoftDelete_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.ClearDomainEvents();
        var version = member.Version;

        member.SoftDelete(_actorId, _now);

        member.Version.Should().Be(version + 1);
        member.IsDeleted.Should().BeTrue();
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRemovedDomainEvent);
    }

    [Fact]
    public void WorkspaceMember_Restore_ShouldIncrementVersion()
    {
        var member = WorkspaceMember.Create(_workspaceId, _userId, WorkspaceRole.Member, _actorId, _now);
        member.SoftDelete(_actorId, _now);
        member.ClearDomainEvents();
        var version = member.Version;

        member.Restore(_actorId, _now);

        member.Version.Should().Be(version + 1);
        member.IsDeleted.Should().BeFalse();
        member.DomainEvents.Should().Contain(e => e is WorkspaceMemberRestoredDomainEvent);
    }

    #endregion

    #region Team

    [Fact]
    public void Team_Rename_ShouldIncrementVersion()
    {
        var team = Team.Create(_workspaceId, "Original", _actorId, _now);
        team.ClearDomainEvents();
        var version = team.Version;

        team.Rename("Renamed", _actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamRenamedDomainEvent);
    }

    [Fact]
    public void Team_Archive_ShouldIncrementVersion()
    {
        var team = Team.Create(_workspaceId, "Team", _actorId, _now);
        team.ClearDomainEvents();
        var version = team.Version;

        team.Archive(_actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamArchivedDomainEvent);
    }

    [Fact]
    public void Team_AddMember_ShouldIncrementVersion()
    {
        var team = Team.Create(_workspaceId, "Team", _actorId, _now);
        team.ClearDomainEvents();
        var version = team.Version;

        team.AddMember(_userId, TeamMemberRole.Member, _actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamMemberAddedDomainEvent);
    }

    [Fact]
    public void Team_RemoveMember_ShouldIncrementVersion()
    {
        var team = Team.Create(_workspaceId, "Team", _actorId, _now);
        team.AddMember(_userId, TeamMemberRole.Member, _actorId, _now);
        team.ClearDomainEvents();
        var version = team.Version;

        team.RemoveMember(_userId, _actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamMemberRemovedDomainEvent);
    }

    [Fact]
    public void Team_SoftDelete_ShouldIncrementVersion()
    {
        var team = Team.Create(_workspaceId, "Team", _actorId, _now);
        team.ClearDomainEvents();
        var version = team.Version;

        team.SoftDelete(_actorId, _now);

        team.Version.Should().Be(version + 1);
        team.IsDeleted.Should().BeTrue();
        team.DomainEvents.Should().Contain(e => e is TeamSoftDeletedDomainEvent);
    }

    [Fact]
    public void Team_Restore_ShouldIncrementVersion()
    {
        var team = Team.Create(_workspaceId, "Team", _actorId, _now);
        team.SoftDelete(_actorId, _now);
        team.ClearDomainEvents();
        var version = team.Version;

        team.Restore(_actorId, _now);

        team.Version.Should().Be(version + 1);
        team.IsDeleted.Should().BeFalse();
        team.DomainEvents.Should().Contain(e => e is TeamRestoredDomainEvent);
    }

    #endregion

    #region Space

    [Fact]
    public void Space_Rename_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Original", SpaceVisibility.Private, _actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.Rename("Renamed", _actorId, _now);

        space.Version.Should().Be(version + 1);
        space.DomainEvents.Should().Contain(e => e is SpaceRenamedDomainEvent);
    }

    [Fact]
    public void Space_Archive_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.Archive(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.DomainEvents.Should().Contain(e => e is SpaceArchivedDomainEvent);
    }

    [Fact]
    public void Space_SoftDelete_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.SoftDelete(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.IsDeleted.Should().BeTrue();
        space.DomainEvents.Should().Contain(e => e is SpaceSoftDeletedDomainEvent);
    }

    [Fact]
    public void Space_Restore_ShouldIncrementVersion()
    {
        var space = Space.Create(_workspaceId, "Space", SpaceVisibility.Private, _actorId, _now);
        space.SoftDelete(_actorId, _now);
        space.ClearDomainEvents();
        var version = space.Version;

        space.Restore(_actorId, _now);

        space.Version.Should().Be(version + 1);
        space.IsDeleted.Should().BeFalse();
        space.DomainEvents.Should().Contain(e => e is SpaceRestoredDomainEvent);
    }

    #endregion

    #region AutomationExecution

    [Fact]
    public void AutomationExecution_SetPayload_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(_workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.SetPayload("{\"key\":\"value\"}");

        execution.Version.Should().Be(version + 1);
    }

    [Fact]
    public void AutomationExecution_Start_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(_workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Start(_now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionStartedDomainEvent);
    }

    [Fact]
    public void AutomationExecution_Succeed_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(_workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.Start(_now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Succeed(_now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionSucceededDomainEvent);
    }

    [Fact]
    public void AutomationExecution_Fail_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(_workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.Start(_now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Fail("error", _now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionFailedDomainEvent);
    }

    [Fact]
    public void AutomationExecution_Cancel_ShouldIncrementVersion()
    {
        var execution = AutomationExecution.Create(_workspaceId, Guid.NewGuid(), Guid.NewGuid(), _now);
        execution.ClearDomainEvents();
        var version = execution.Version;

        execution.Cancel(_actorId, _now);

        execution.Version.Should().Be(version + 1);
        execution.DomainEvents.Should().Contain(e => e is AutomationExecutionCancelledDomainEvent);
    }

    #endregion

    #region AiAgentRun

    [Fact]
    public void AiAgentRun_Start_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Start(_now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunStartedDomainEvent);
    }

    [Fact]
    public void AiAgentRun_Succeed_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.Start(_now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Succeed(JsonValue.Null(), _now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunSucceededDomainEvent);
    }

    [Fact]
    public void AiAgentRun_Fail_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.Start(_now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Fail(JsonValue.Null(), _now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunFailedDomainEvent);
    }

    [Fact]
    public void AiAgentRun_Cancel_ShouldIncrementVersion()
    {
        var run = AiAgentRun.Create(_workspaceId, Guid.NewGuid(), "webhook", null, null, JsonValue.Null(), _actorId, null, _now);
        run.Start(_now);
        run.ClearDomainEvents();
        var version = run.Version;

        run.Cancel(_actorId, _now);

        run.Version.Should().Be(version + 1);
        run.DomainEvents.Should().Contain(e => e is AiAgentRunCancelledDomainEvent);
    }

    #endregion

    #region Plan

    [Fact]
    public void Plan_AddLimit_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.AddLimit(FeatureCode.Create("seats"), 10, _now);

        plan.Version.Should().Be(version + 1);
    }

    [Fact]
    public void Plan_UpdateDescription_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.UpdateDescription("New desc", _now);

        plan.Version.Should().Be(version + 1);
    }

    [Fact]
    public void Plan_Archive_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Archive(_now);

        plan.Version.Should().Be(version + 1);
    }

    [Fact]
    public void Plan_Deprecate_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        plan.ClearDomainEvents();
        var version = plan.Version;

        plan.Deprecate(_now);

        plan.Version.Should().Be(version + 1);
    }

    #endregion

    #region Notification

    [Fact]
    public void Notification_MarkAsRead_ShouldIncrementVersion()
    {
        var notification = Notification.Create(_userId, _workspaceId, NotificationType.Mention, "Title", "Content", _now);
        notification.ClearDomainEvents();
        var version = notification.Version;

        notification.MarkAsRead(_now);

        notification.Version.Should().Be(version + 1);
        notification.DomainEvents.Should().Contain(e => e is NotificationReadDomainEvent);
    }

    [Fact]
    public void Notification_Archive_ShouldIncrementVersion()
    {
        var notification = Notification.Create(_userId, _workspaceId, NotificationType.Mention, "Title", "Content", _now);
        notification.ClearDomainEvents();
        var version = notification.Version;

        notification.Archive(_now);

        notification.Version.Should().Be(version + 1);
        notification.DomainEvents.Should().Contain(e => e is NotificationArchivedDomainEvent);
    }

    #endregion
}
