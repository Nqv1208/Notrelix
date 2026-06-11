using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Spaces;
using Xunit;

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
        space.DomainEvents.Should().ContainSingle(e => e is SpaceCreatedEvent);
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
    public void Move_ShouldSucceed_AndRaiseEvent()
    {
        var oldWorkspaceId = Guid.NewGuid();
        var newWorkspaceId = Guid.NewGuid();
        var space = Space.Create(oldWorkspaceId, "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.ClearDomainEvents();

        var movedBy = Guid.NewGuid();
        space.Move(newWorkspaceId, movedBy, DateTimeOffset.UtcNow);

        space.WorkspaceId.Should().Be(newWorkspaceId);
        space.DomainEvents.Should().ContainSingle(e => e is SpaceMovedEvent);
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
    public void SoftDelete_ShouldSetStatusToSoftDeleted()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Status.Should().Be(SpaceStatus.SoftDeleted);
        space.IsDeleted.Should().BeTrue();
        space.DomainEvents.Should().Contain(e => e is SpaceSoftDeletedEvent);
    }

    [Fact]
    public void Restore_ShouldSetStatusToActive()
    {
        var space = Space.Create(Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.ClearDomainEvents();

        space.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Status.Should().Be(SpaceStatus.Active);
        space.IsDeleted.Should().BeFalse();
        space.DomainEvents.Should().Contain(e => e is SpaceRestoredEvent);
    }
}
