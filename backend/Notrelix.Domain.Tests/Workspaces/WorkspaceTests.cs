using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceTests
{
    [Fact]
    public void Create_ShouldSucceed_AndAddOwnerAsFirstMember()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "My Workspace", "my-workspace");

        workspace.Name.Should().Be("My Workspace");
        workspace.Slug.Should().Be("my-workspace");
        workspace.WorkspaceMembers.Should().HaveCount(1);
        workspace.WorkspaceMembers.First().UserId.Should().Be(ownerId);
        workspace.WorkspaceMembers.First().Role.Should().Be(WorkspaceRole.Owner);
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceCreatedEvent);
    }

    [Fact]
    public void AddMember_ShouldAddToList_AndRaiseEvent()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "WS", "ws");
        var userId = Guid.NewGuid();
        var addedBy = Guid.NewGuid();
        workspace.ClearDomainEvents();

        workspace.AddMember(userId, WorkspaceRole.Member, addedBy);

        workspace.WorkspaceMembers.Should().HaveCount(2);
        workspace.WorkspaceMembers.Any(m => m.UserId == userId).Should().BeTrue();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberAddedEvent);
    }

    [Fact]
    public void RemoveMember_ShouldThrow_WhenRemovingLastOwner()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "WS", "ws");

        Action act = () => workspace.RemoveMember(ownerId, ownerId);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot remove the last owner of the workspace.");
    }

    [Fact]
    public void ChangeMemberRole_ShouldThrow_WhenDowngradingLastOwner()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "WS", "ws");

        Action act = () => workspace.ChangeMemberRole(ownerId, WorkspaceRole.Member, ownerId);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot downgrade the role of the last owner.");
    }
}
