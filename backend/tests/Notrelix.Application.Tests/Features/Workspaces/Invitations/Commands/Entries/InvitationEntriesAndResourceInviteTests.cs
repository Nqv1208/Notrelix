using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.Entries;

/// <summary>WSP-026: authorization shape and request validation of the two invitation flows.</summary>
public class InvitationEntriesAndResourceInviteTests
{
    private static InviteMemberCommand Invite(params InvitationInitialEntryInput[] entries) =>
        new(Guid.NewGuid(), "a@example.com", WorkspaceRole.Member, entries);

    [Fact]
    public void InviteMember_WithoutEntries_IsAuthorizedAsInviteMember() =>
        ((IRequirePermission)Invite()).Action.Should().Be(PermissionAction.InviteMember);

    [Fact]
    public void InviteMember_WithTeamEntries_IsAuthorizedAsManageTeams() =>
        ((IRequirePermission)Invite(new InvitationInitialEntryInput("Team", Guid.NewGuid(), "Member"), new InvitationInitialEntryInput("Space", Guid.NewGuid(), "Viewer")))
            .Action.Should().Be(PermissionAction.ManageTeams);

    [Fact]
    public void InviteMember_WithSpaceOnlyEntries_IsAuthorizedAsManageSpaces() =>
        ((IRequirePermission)Invite(new InvitationInitialEntryInput("Space", Guid.NewGuid(), "Viewer")))
            .Action.Should().Be(PermissionAction.ManageSpaces);

    [Fact]
    public void InviteMember_Validator_RejectsEmptyTargetAndTooManyEntries()
    {
        var validator = new InviteMemberCommandValidator();
        validator.Validate(Invite(new InvitationInitialEntryInput("Team", Guid.Empty, "Member"))).IsValid.Should().BeFalse();
        var many = Enumerable.Range(0, 21).Select(_ => new InvitationInitialEntryInput("Space", Guid.NewGuid(), "Viewer")).ToArray();
        validator.Validate(Invite(many)).IsValid.Should().BeFalse();
        validator.Validate(Invite(new InvitationInitialEntryInput("Team", Guid.NewGuid(), "Member"))).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("work-management.board", "ManageBoardPermission")]
    [InlineData("documents.page", "ManagePagePermission")]
    public void ResourceInvitation_IsAuthorizedAsManagePermissionOnTheResource(string kind, string action)
    {
        var command = new InviteGuestToResourceCommand(kind, Guid.NewGuid(), "a@example.com", "Editor");

        ((IRequirePermission)command).Action.ToString().Should().Be(action);
        ((IRequirePermission)command).Resource.Kind.Value.Should().Be(kind);
    }

    [Theory]
    [InlineData("Viewer", 1)]
    [InlineData("Commenter", 2)]
    [InlineData("editor", 3)]
    [InlineData("Manager", 4)]
    [InlineData("nonsense", 0)]
    public void ResourceInvitation_RequestsTheGovernanceRankOfItsLevel(string level, int rank) =>
        ((IRequireGrantPermission)new InviteGuestToResourceCommand("work-management.board", Guid.NewGuid(), "a@example.com", level))
            .RequestedPermissionRank.Should().Be(rank);

    [Theory]
    [InlineData("work-management.board-item", false)]
    [InlineData("workspaces.space", false)]
    [InlineData("documents.page", true)]
    public void ResourceInvitation_Validator_AcceptsOnlyBoardAndPage(string kind, bool valid) =>
        new InviteGuestToResourceCommandValidator()
            .Validate(new InviteGuestToResourceCommand(kind, Guid.NewGuid(), "a@example.com", "Viewer"))
            .IsValid.Should().Be(valid);
}
