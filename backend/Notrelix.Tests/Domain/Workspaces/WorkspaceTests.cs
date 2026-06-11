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
    public void Create_ShouldSucceed()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(ownerId, "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        workspace.Name.Should().Be("My Workspace");
        workspace.Slug.Should().Be("my-workspace");
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceCreatedEvent);
    }

    [Fact]
    public void CreateMember_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var addedBy = Guid.NewGuid();
        
        var member = WorkspaceMember.Create(workspaceId, userId, WorkspaceRole.Member, addedBy, DateTimeOffset.UtcNow);

        member.WorkspaceId.Should().Be(workspaceId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(WorkspaceRole.Member);
        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberAddedEvent);
    }

    [Fact]
    public void ChangeMemberRole_ShouldChangeRole_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.ClearDomainEvents();

        member.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.Role.Should().Be(WorkspaceRole.Admin);
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRoleChangedEvent);
    }

    [Fact]
    public void RemoveMember_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var member = WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.ClearDomainEvents();

        member.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.IsDeleted.Should().BeTrue();
        member.DomainEvents.Should().ContainSingle(e => e is WorkspaceMemberRemovedEvent);
    }
}
