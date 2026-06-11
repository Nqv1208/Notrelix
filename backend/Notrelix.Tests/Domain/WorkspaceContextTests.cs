using FluentAssertions;
using Notrelix.Domain.Workspaces.Events;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Domain.Tests;

public class WorkspaceContextTests
{
    [Fact]
    public void Workspace_SoftDelete_ShouldSetStateAndRaiseEvent()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team WS", ownerId);
        workspace.ClearDomainEvents();
        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTime.UtcNow;

        // Act
        workspace.SoftDelete(deletedBy, deletedAt, "No longer active");

        // Assert
        workspace.IsDeleted.Should().BeTrue();
        workspace.DeletedAt.Should().Be(deletedAt);
        workspace.DeletedBy.Should().Be(deletedBy);
        workspace.DeleteReason.Should().Be("No longer active");

        workspace.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceDeletedEvent>();

        var evt = (WorkspaceDeletedEvent)workspace.DomainEvents.Single();
        evt.WorkspaceId.Should().Be(workspace.Id);
        evt.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void Workspace_Restore_ShouldResetStateAndRaiseEvent()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.CreateTeam("Team WS", ownerId);
        workspace.SoftDelete(Guid.NewGuid(), DateTime.UtcNow, "Reason");
        workspace.ClearDomainEvents();
        var restoredBy = Guid.NewGuid();
        var restoredAt = DateTime.UtcNow;

        // Act
        workspace.Restore(restoredBy, restoredAt);

        // Assert
        workspace.IsDeleted.Should().BeFalse();
        workspace.DeletedAt.Should().BeNull();
        workspace.DeletedBy.Should().BeNull();
        workspace.RestoredAt.Should().Be(restoredAt);
        workspace.RestoredBy.Should().Be(restoredBy);

        workspace.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceRestoredEvent>();

        var evt = (WorkspaceRestoredEvent)workspace.DomainEvents.Single();
        evt.WorkspaceId.Should().Be(workspace.Id);
        evt.RestoredBy.Should().Be(restoredBy);
    }

    [Fact]
    public void Space_CreateAndManipulate_ShouldWorkAsExpected()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var creator = Guid.NewGuid();

        // Act - Create
        var space = Space.Create(
            workspaceId,
            "Engineering Space",
            null,
            1.0,
            SpaceVisibility.Workspace,
            creator,
            "Description of Space",
            "💻",
            "#00FF00");

        // Assert - Create
        space.WorkspaceId.Should().Be(workspaceId);
        space.Name.Should().Be("Engineering Space");
        space.ParentSpaceId.Should().BeNull();
        space.Position.Should().Be(1.0);
        space.Visibility.Should().Be(SpaceVisibility.Workspace);
        space.Status.Should().Be(SpaceStatus.Active);
        space.Description.Should().Be("Description of Space");
        space.Icon.Should().Be("💻");
        space.Color.Should().Be("#00FF00");
        space.CreatedBy.Should().Be(creator);

        // Act - Move
        var parentId = Guid.NewGuid();
        space.Move(parentId, 2.5, creator);

        // Assert - Move
        space.ParentSpaceId.Should().Be(parentId);
        space.Position.Should().Be(2.5);
        space.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SpaceMovedEvent>();

        var moveEvt = (SpaceMovedEvent)space.DomainEvents.Single();
        moveEvt.SpaceId.Should().Be(space.Id);
        moveEvt.ParentSpaceId.Should().Be(parentId);
        moveEvt.Position.Should().Be(2.5);

        // Act - Archive
        space.ClearDomainEvents();
        space.Archive(creator);

        // Assert - Archive
        space.Status.Should().Be(SpaceStatus.Archived);
        space.ArchivedAt.Should().NotBeNull();
        space.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SpaceArchivedEvent>();

        var archiveEvt = (SpaceArchivedEvent)space.DomainEvents.Single();
        archiveEvt.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void Team_CreateAndManageMembers_ShouldRespectInvariants()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var creator = Guid.NewGuid();
        var team = Team.Create(workspaceId, "Product Team", creator, "Description", "#123456", "url");

        // Assert - Create
        team.WorkspaceId.Should().Be(workspaceId);
        team.Name.Should().Be("Product Team");
        team.Description.Should().Be("Description");
        team.Color.Should().Be("#123456");
        team.AvatarUrl.Should().Be("url");
        team.Status.Should().Be(TeamStatus.Active);
        team.CreatedBy.Should().Be(creator);
        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamCreatedEvent>();

        // Act - Add Member
        var userId = Guid.NewGuid();
        team.ClearDomainEvents();
        var member = team.AddMember(userId, TeamMemberRole.Manager, creator);

        // Assert - Add Member
        team.Members.Should().ContainSingle();
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(TeamMemberRole.Manager);
        member.TeamId.Should().Be(team.Id);
        member.CreatedBy.Should().Be(creator);

        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamMemberAddedEvent>();

        var addEvt = (TeamMemberAddedEvent)team.DomainEvents.Single();
        addEvt.UserId.Should().Be(userId);
        addEvt.Role.Should().Be(TeamMemberRole.Manager);

        // Act - Update Member Role
        team.UpdateMemberRole(userId, TeamMemberRole.Member, creator);
        member.Role.Should().Be(TeamMemberRole.Member);

        // Act - Remove Member
        team.ClearDomainEvents();
        team.RemoveMember(userId, creator);

        // Assert - Remove Member
        team.Members.Should().BeEmpty();
        team.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TeamMemberRemovedEvent>();

        var removeEvt = (TeamMemberRemovedEvent)team.DomainEvents.Single();
        removeEvt.UserId.Should().Be(userId);
    }
}
