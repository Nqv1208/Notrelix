using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Common.Behaviors;

/// <summary>
/// WSP-026 / GOV-028: an invitation naming both Team and Space entries needs both
/// ManageTeams and ManageSpaces. The pipeline (IRequireAdditionalPermission) checks
/// each action over its own facts and rules; the handler has no role check.
/// </summary>
public sealed class AdditionalPermissionBehaviorTests
{
    private static readonly InvitationInitialEntryInput Team = new("Team", Guid.NewGuid(), "Member");
    private static readonly InvitationInitialEntryInput Space = new("Space", Guid.NewGuid(), "Viewer");

    private static InviteMemberCommand Invite(params InvitationInitialEntryInput[] entries) =>
        new(Guid.NewGuid(), "a@example.com", WorkspaceRole.Member, entries);

    [Fact]
    public void InviteMember_NamingBothKinds_RequiresBothActions()
    {
        var both = (IRequireAdditionalPermission)Invite(Team, Space);
        ((IRequirePermission)both).Action.Should().Be(PermissionAction.ManageTeams);
        both.AdditionalActionNames.Should().Equal(nameof(PermissionAction.ManageSpaces));

        ((IRequireAdditionalPermission)Invite(Team)).AdditionalActionNames.Should().BeEmpty();
        ((IRequireAdditionalPermission)Invite(Space)).AdditionalActionNames.Should().BeEmpty();
        ((IRequireAdditionalPermission)Invite()).AdditionalActionNames.Should().BeEmpty();
    }

    // The user is a Workspace Member (no default authority over Teams or Spaces)
    // holding custom Allow rules for exactly the listed actions.
    [Theory]
    [InlineData("Both", "ManageTeams", false)]
    [InlineData("Both", "ManageSpaces", false)]
    [InlineData("Both", "ManageTeams,ManageSpaces", true)]
    [InlineData("Team", "ManageTeams", true)]
    [InlineData("Team", "ManageSpaces", false)]
    [InlineData("Space", "ManageSpaces", true)]
    [InlineData("Space", "ManageTeams", false)]
    public async Task CustomRules_AuthorizeOnlyTheActionsTheInvitationNeeds(string names, string granted, bool expectedAllowed)
    {
        var entries = names switch
        {
            "Both" => new[] { Team, Space },
            "Team" => new[] { Team },
            _ => new[] { Space },
        };
        var behavior = CreateBehavior(granted.Split(','), out var calls);

        var act = () => behavior.Handle(Invite(entries), _ => Task.FromResult("ok"), CancellationToken.None);

        if (expectedAllowed)
            (await act()).Should().Be("ok");
        else
            await act.Should().ThrowAsync<ForbiddenException>();

        calls.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WorkspaceAdmin_NamingBothKinds_IsAllowedByDefault()
    {
        var behavior = CreateBehavior([], out _, role: "Admin");

        (await behavior.Handle(Invite(Team, Space), _ => Task.FromResult("ok"), CancellationToken.None))
            .Should().Be("ok");
    }

    [Fact]
    public async Task WorkspaceMember_NamingBothKinds_IsForbiddenAndHandlerDoesNotRun()
    {
        var behavior = CreateBehavior([], out _);
        var handlerRan = false;

        var act = () => behavior.Handle(Invite(Team, Space), _ => { handlerRan = true; return Task.FromResult("ok"); }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        handlerRan.Should().BeFalse();
    }

    [Fact]
    public async Task DenyRuleOnTheAdditionalAction_BlocksAnAdmin()
    {
        var behavior = CreateBehavior([], out _, role: "Admin", denied: ["ManageSpaces"]);

        var act = () => behavior.Handle(Invite(Team, Space), _ => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static AccessControlBehavior<InviteMemberCommand, string> CreateBehavior(
        string[] granted,
        out List<string> calls,
        string role = "Member",
        string[]? denied = null)
    {
        var descriptor = RequestDescriptorValidator.Create(typeof(InviteMemberCommand));
        var descriptors = new Mock<IRequestDescriptorRegistry>();
        descriptors.Setup(r => r.GetRequired(typeof(InviteMemberCommand))).Returns(descriptor);
        var context = new Mock<IExecutionContextReader>();
        context.SetupGet(r => r.Snapshot).Returns(new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            descriptor.Principal, descriptor.Scope, Guid.NewGuid().ToString("D")));

        var recorded = new List<string>();
        calls = recorded;
        var provider = new Mock<IAccessFactsProvider>();
        provider.Setup(p => p.ResolveAsync(
                It.IsAny<RequestDescriptor>(), It.IsAny<ExecutionContextSnapshot>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestDescriptor _, ExecutionContextSnapshot _, object request, CancellationToken _) =>
            {
                var action = ((IRequirePermission)request).Action.ToString();
                recorded.Add(action);
                var rules = new List<AccessPermissionRule>();
                if (granted.Contains(action)) rules.Add(new AccessPermissionRule(1, "Allow"));
                if (denied?.Contains(action) == true) rules.Add(new AccessPermissionRule(1, "Deny"));
                return new AccessFacts(true, true, true, "Member", true, role, false, null, null, false,
                    rules, false, false, null, null);
            });

        return new AccessControlBehavior<InviteMemberCommand, string>(
            descriptors.Object,
            context.Object,
            provider.Object,
            new AccessPolicyEngine(),
            new PipelineMetrics(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AccessControlBehavior<InviteMemberCommand, string>>.Instance);
    }
}
