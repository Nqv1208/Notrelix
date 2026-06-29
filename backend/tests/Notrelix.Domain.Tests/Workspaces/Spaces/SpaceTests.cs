using FluentAssertions;
using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Domain.Tests.Workspaces;

public class SpaceTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var space = Space.Create(workspaceId, "Marketing", SpaceVisibility.Workspace, createdBy, DateTimeOffset.UtcNow);

        space.WorkspaceId.Should().Be(workspaceId);
        space.Name.Should().Be("Marketing");
        space.Visibility.Should().Be(SpaceVisibility.Workspace);
        space.Status.Should().Be(SpaceStatus.Active);
        space.DomainEvents.Should().ContainSingle(e => e is SpaceCreatedDomainEvent);
    }

    [Fact]
    public void Rename_ShouldSucceed()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var updatedBy = Guid.NewGuid();

        space.Rename("Sales", updatedBy, DateTimeOffset.UtcNow);

        space.Name.Should().Be("Sales");
    }

    [Fact]
    public void Move_CrossWorkspace_ShouldThrow()
    {
        var oldWorkspaceId = Guid.NewGuid();
        var newWorkspaceId = Guid.NewGuid();
        var space = Space.Create(oldWorkspaceId, "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Move(newWorkspaceId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*not allowed*");
    }

    [Fact]
    public void Move_SameWorkspace_ShouldBeNoOp()
    {
        var workspaceId = Guid.NewGuid();
        var space = Space.Create(workspaceId, "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.ClearDomainEvents();

        space.Move(workspaceId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.WorkspaceId.Should().Be(workspaceId);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rename_ShouldThrow_WhenArchived()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Rename("Sales", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Move_ShouldThrow_WhenArchived()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var newWorkspaceId = Guid.NewGuid();
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Move(newWorkspaceId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void SoftDelete_ShouldSetStatusToSoftDeleted_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Status.Should().Be(SpaceStatus.SoftDeleted);
        space.IsDeleted.Should().BeTrue();
        space.DomainEvents.Should().Contain(e => e is SpaceSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.ClearDomainEvents();

        space.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Status.Should().Be(SpaceStatus.Active);
        space.IsDeleted.Should().BeFalse();
        space.DomainEvents.Should().Contain(e => e is SpaceRestoredDomainEvent);
    }

    [Fact]
    public void Rename_OnDeletedSpace_ShouldThrow()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Rename("Sales", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted and cannot be modified*");
    }
}
