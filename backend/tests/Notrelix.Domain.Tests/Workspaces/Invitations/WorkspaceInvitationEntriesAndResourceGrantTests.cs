using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces;

/// <summary>WSP-026 / GOV-030: initial entries and the resource grant of an invitation.</summary>
public class WorkspaceInvitationEntriesAndResourceGrantTests
{
    private const string Hash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    private static WorkspaceInvitation Workspace(WorkspaceRole role, params WorkspaceInvitationInitialEntry[] entries) =>
        WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "a@example.com", role,
            InvitationTokenHash.Create(Hash), 1, Guid.NewGuid(), Now, initialEntries: entries);

    private static WorkspaceInvitation Resource(string level = "Editor", string kind = WorkspaceInvitationResourceGrant.BoardKind) =>
        WorkspaceInvitation.CreateForResource(Guid.NewGuid(), Guid.NewGuid(), "a@example.com",
            new WorkspaceInvitationResourceGrant(kind, Guid.NewGuid(), level),
            InvitationTokenHash.Create(Hash), 1, Guid.NewGuid(), Now);

    private static WorkspaceInvitationInitialEntry Team(string role = "Member") =>
        new(WorkspaceInvitationEntryKind.Team, Guid.NewGuid(), role);

    private static WorkspaceInvitationInitialEntry Space(string role = "Viewer") =>
        new(WorkspaceInvitationEntryKind.Space, Guid.NewGuid(), role);

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public void Create_StoresCanonicalInitialEntries_ForMemberOrGuest(WorkspaceRole role)
    {
        var invitation = Workspace(role, Team("member"), Space("viewer"));

        invitation.InitialEntries.Should().HaveCount(2);
        invitation.InitialEntries.Select(e => e.Role).Should().Equal("Member", "Viewer");
        invitation.IsResourceInvitation.Should().BeFalse();
    }

    [Fact]
    public void Create_RejectsEntries_ForAdminInvitation() =>
        FluentActions.Invoking(() => Workspace(WorkspaceRole.Admin, Team()))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_InitialEntriesRequireMemberOrGuest);

    [Theory]
    [InlineData("Lead", true)]
    [InlineData("Admin", false)]
    public void Create_RejectsTeamLeadAndSpaceAdmin_ForGuestInvitation(string role, bool team) =>
        FluentActions.Invoking(() => Workspace(WorkspaceRole.Guest, team ? Team(role) : Space(role)))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_GuestEntryRoleTooPrivileged);

    [Fact]
    public void Create_AllowsTeamLeadAndSpaceAdmin_ForMemberInvitation() =>
        Workspace(WorkspaceRole.Member, Team("Lead"), Space("Admin")).InitialEntries.Should().HaveCount(2);

    [Fact]
    public void Entry_RejectsUnknownRoleAndEmptyTarget()
    {
        FluentActions.Invoking(() => new WorkspaceInvitationInitialEntry(WorkspaceInvitationEntryKind.Team, Guid.NewGuid(), "Owner"))
            .Should().Throw<BusinessRuleException>();
        FluentActions.Invoking(() => new WorkspaceInvitationInitialEntry(WorkspaceInvitationEntryKind.Space, Guid.Empty, "Viewer"))
            .Should().Throw<BusinessRuleException>();
        FluentActions.Invoking(() => WorkspaceInvitationInitialEntry.Parse("Board", Guid.NewGuid(), "Viewer"))
            .Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_RejectsDuplicateTargetAndTooManyEntries()
    {
        var team = Team();
        FluentActions.Invoking(() => Workspace(WorkspaceRole.Member, team, new WorkspaceInvitationInitialEntry(WorkspaceInvitationEntryKind.Team, team.TargetId, "Lead")))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_InitialEntryDuplicate);

        var many = Enumerable.Range(0, WorkspaceInvitationInitialEntry.MaxEntries + 1).Select(_ => Space()).ToArray();
        FluentActions.Invoking(() => Workspace(WorkspaceRole.Member, many))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_TooManyInitialEntries);
    }

    [Fact]
    public void ChangeRole_ToAdmin_IsRejectedAndLeavesStateUnchanged_WhenEntriesExist()
    {
        var invitation = Workspace(WorkspaceRole.Member, Team());
        var version = invitation.Version;

        FluentActions.Invoking(() => invitation.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), Now))
            .Should().Throw<BusinessRuleException>();

        invitation.Role.Should().Be(WorkspaceRole.Member);
        invitation.Version.Should().Be(version);
    }

    [Fact]
    public void ChangeRole_ToGuest_IsRejected_WhenEntriesNameTeamLead()
    {
        var invitation = Workspace(WorkspaceRole.Member, Team("Lead"));

        FluentActions.Invoking(() => invitation.ChangeRole(WorkspaceRole.Guest, Guid.NewGuid(), Now))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_GuestEntryRoleTooPrivileged);
        invitation.Role.Should().Be(WorkspaceRole.Member);
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("commenter")]
    [InlineData("Editor")]
    public void ResourceInvitation_IsGuestOnly_WithCanonicalLevel(string level)
    {
        var invitation = Resource(level);

        invitation.Role.Should().Be(WorkspaceRole.Guest);
        invitation.IsResourceInvitation.Should().BeTrue();
        invitation.InitialEntries.Should().BeEmpty();
        invitation.ResourceGrant!.Level.Should().BeOneOf("Viewer", "Commenter", "Editor");
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Owner")]
    [InlineData("None")]
    [InlineData("")]
    public void ResourceGrant_RejectsLevelAboveEditor(string level) =>
        FluentActions.Invoking(() => new WorkspaceInvitationResourceGrant(WorkspaceInvitationResourceGrant.BoardKind, Guid.NewGuid(), level))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_ResourceLevelAboveEditor);

    [Theory]
    [InlineData("work-management.board-item")]
    [InlineData("workspaces.space")]
    [InlineData("governance.share-link")]
    public void ResourceGrant_RejectsKindsThatAreNotBoardOrPage(string kind) =>
        FluentActions.Invoking(() => new WorkspaceInvitationResourceGrant(kind, Guid.NewGuid(), "Viewer"))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_ResourceKindNotInvitable);

    [Fact]
    public void ResourceInvitation_CannotBecomeMemberOrAdmin_AndFailureLeavesStateUnchanged()
    {
        var invitation = Resource();
        var version = invitation.Version;

        FluentActions.Invoking(() => invitation.ChangeRole(WorkspaceRole.Member, Guid.NewGuid(), Now))
            .Should().Throw<BusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_ResourceInvitationMustBeGuest);

        invitation.Role.Should().Be(WorkspaceRole.Guest);
        invitation.Version.Should().Be(version);
    }

    [Fact]
    public void ResourceInvitation_StillAcceptsOnlyOnceAndExpires()
    {
        var invitation = Resource();
        var user = Guid.NewGuid();

        FluentActions.Invoking(() => invitation.Accept(user, Now.AddDays(8)))
            .Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);

        invitation.Accept(user, Now.AddHours(1));
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        invitation.Accept(user, Now.AddHours(2));
        invitation.DomainEvents.OfType<Notrelix.Domain.Workspaces.Invitations.Events.WorkspaceInvitationAcceptedDomainEvent>()
            .Should().ContainSingle("a repeated accept is a no-op");
    }
}
