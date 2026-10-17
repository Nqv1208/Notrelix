using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ArchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;
using Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RestoreTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UnarchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UpdateTeamDescription;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams.Commands;

/// <summary>
/// Request contract of the Team writes: a Team role outside Lead/Member fails
/// validation before any handler runs, a version-less call is rejected, every
/// versioned Team write is authorized as ManageTeams on the Workspace while its
/// expected version binds to the Team aggregate, and Restore stays version-less
/// (a deleted Team is addressed by identity, like RestoreSpace).
/// </summary>
public sealed class TeamWriteContractTests
{
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid TeamId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Theory]
    [InlineData("Owner")]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("Lead,Member")]
    public void AddTeamMember_UnknownRole_FailsValidation(string role)
    {
        var result = new AddTeamMemberCommandValidator()
            .Validate(new AddTeamMemberCommand(WorkspaceId, TeamId, UserId, role, 1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddTeamMemberCommand.Role));
    }

    [Theory]
    [InlineData("Lead")]
    [InlineData("member")]
    public void AddTeamMember_KnownRole_PassesValidation_CaseInsensitively(string role) =>
        new AddTeamMemberCommandValidator()
            .Validate(new AddTeamMemberCommand(WorkspaceId, TeamId, UserId, role, 1))
            .IsValid.Should().BeTrue();

    [Fact]
    public void ChangeTeamMemberRole_UnknownRole_FailsValidation()
    {
        var result = new ChangeTeamMemberRoleCommandValidator()
            .Validate(new ChangeTeamMemberRoleCommand(WorkspaceId, TeamId, UserId, "Owner", 1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangeTeamMemberRoleCommand.NewRole));
    }

    [Fact]
    public void ChangeTeamMemberRole_KnownRole_PassesValidation() =>
        new ChangeTeamMemberRoleCommandValidator()
            .Validate(new ChangeTeamMemberRoleCommand(WorkspaceId, TeamId, UserId, "lead", 1))
            .IsValid.Should().BeTrue();

    [Fact]
    public void VersionedTeamWrites_RejectANonPositiveExpectedVersion()
    {
        new RenameTeamCommandValidator().Validate(new RenameTeamCommand(WorkspaceId, TeamId, "Name", 0)).IsValid.Should().BeFalse();
        new UpdateTeamDescriptionCommandValidator().Validate(new UpdateTeamDescriptionCommand(WorkspaceId, TeamId, "d", 0)).IsValid.Should().BeFalse();
        new ArchiveTeamCommandValidator().Validate(new ArchiveTeamCommand(WorkspaceId, TeamId, 0)).IsValid.Should().BeFalse();
        new UnarchiveTeamCommandValidator().Validate(new UnarchiveTeamCommand(WorkspaceId, TeamId, 0)).IsValid.Should().BeFalse();
        new DeleteTeamCommandValidator().Validate(new DeleteTeamCommand(WorkspaceId, TeamId, 0)).IsValid.Should().BeFalse();
        new AddTeamMemberCommandValidator().Validate(new AddTeamMemberCommand(WorkspaceId, TeamId, UserId, "Member", 0)).IsValid.Should().BeFalse();
        new RemoveTeamMemberCommandValidator().Validate(new RemoveTeamMemberCommand(WorkspaceId, TeamId, UserId, 0)).IsValid.Should().BeFalse();
        new ChangeTeamMemberRoleCommandValidator().Validate(new ChangeTeamMemberRoleCommand(WorkspaceId, TeamId, UserId, "Lead", 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void VersionedTeamWrites_AreAuthorizedOnTheWorkspace_AndVersionBoundToTheTeam()
    {
        IExpectedVersionRequest[] versioned =
        [
            new RenameTeamCommand(WorkspaceId, TeamId, "Name", 3),
            new UpdateTeamDescriptionCommand(WorkspaceId, TeamId, "d", 3),
            new ArchiveTeamCommand(WorkspaceId, TeamId, 3),
            new UnarchiveTeamCommand(WorkspaceId, TeamId, 3),
            new DeleteTeamCommand(WorkspaceId, TeamId, 3),
            new AddTeamMemberCommand(WorkspaceId, TeamId, UserId, "Member", 3),
            new RemoveTeamMemberCommand(WorkspaceId, TeamId, UserId, 3),
            new ChangeTeamMemberRoleCommand(WorkspaceId, TeamId, UserId, "Lead", 3),
        ];

        foreach (var request in versioned)
        {
            request.ExpectedVersion.Should().Be(3, request.GetType().Name);
            request.Resource.Kind.Value.Should().Be("workspaces.team", request.GetType().Name);
            request.Resource.ResourceId.Should().Be(TeamId, request.GetType().Name);

            var permission = (IRequirePermission)request;
            permission.Action.Should().Be(PermissionAction.ManageTeams, request.GetType().Name);
            permission.Resource.Kind.Value.Should().Be("workspaces.workspace", request.GetType().Name);
            permission.Resource.ResourceId.Should().Be(WorkspaceId, request.GetType().Name);
        }
    }

    [Fact]
    public void RestoreTeam_StaysVersionless_AndManageTeams()
    {
        var command = new RestoreTeamCommand(WorkspaceId, TeamId);

        command.Should().NotBeAssignableTo<IExpectedVersionRequest>();
        command.Action.Should().Be(PermissionAction.ManageTeams);
    }
}
