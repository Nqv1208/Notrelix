using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class SpaceTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var space = Space.Create(Guid.NewGuid(), workspaceId, "Marketing", SpaceVisibility.Workspace, createdBy, DateTimeOffset.UtcNow);

        space.WorkspaceId.Should().Be(workspaceId);
        space.Name.Should().Be("Marketing");
        space.Visibility.Should().Be(SpaceVisibility.Workspace);
        space.Status.Should().Be(SpaceStatus.Active);
        space.DomainEvents.Should().ContainSingle(e => e is SpaceCreatedDomainEvent);
    }

    [Fact]
    public void Rename_ShouldSucceed()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var updatedBy = Guid.NewGuid();

        space.Rename("Sales", updatedBy, DateTimeOffset.UtcNow);

        space.Name.Should().Be("Sales");
    }

    [Fact]
    public void Rename_ShouldThrow_WhenArchived()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Rename("Sales", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Unarchive_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var actor = Guid.NewGuid();

        space.Unarchive(actor, DateTimeOffset.UtcNow);

        space.Status.Should().Be(SpaceStatus.Active);
        space.DomainEvents.Should().ContainSingle(e => e is SpaceUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_WhenAlreadyActive_ShouldBeNoOp()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();

        space.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Status.Should().Be(SpaceStatus.Active);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_Deleted_ShouldThrow()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void UpdateDescription_ShouldSucceed_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var actor = Guid.NewGuid();

        space.UpdateDescription("Campaign tracking", actor, DateTimeOffset.UtcNow);

        space.Description.Should().Be("Campaign tracking");
        space.DomainEvents.Should().ContainSingle(e => e is SpaceDescriptionUpdatedDomainEvent);
        var evt = (SpaceDescriptionUpdatedDomainEvent)space.DomainEvents.First();
        evt.OldDescription.Should().BeNull();
        evt.NewDescription.Should().Be("Campaign tracking");
        evt.UpdatedBy.Should().Be(actor);
    }

    [Fact]
    public void UpdateDescription_ShouldClearDescription_WhenSetToNull()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow, description: "Initial");
        ((IHasDomainEvents)space).ClearDomainEvents();

        space.UpdateDescription(null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Description.Should().BeNull();
        space.DomainEvents.Should().ContainSingle(e => e is SpaceDescriptionUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateDescription_WhenSameValue_ShouldBeNoOp()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow, description: "Same");
        ((IHasDomainEvents)space).ClearDomainEvents();

        space.UpdateDescription("Same", Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDescription_ArchivedSpace_ShouldThrow()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.UpdateDescription("New", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void ChangeVisibility_ShouldSucceed_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var actor = Guid.NewGuid();

        space.ChangeVisibility(SpaceVisibility.Private, actor, DateTimeOffset.UtcNow);

        space.Visibility.Should().Be(SpaceVisibility.Private);
        space.DomainEvents.Should().ContainSingle(e => e is SpaceVisibilityChangedDomainEvent);
        var evt = (SpaceVisibilityChangedDomainEvent)space.DomainEvents.First();
        evt.OldVisibility.Should().Be(SpaceVisibility.Workspace);
        evt.NewVisibility.Should().Be(SpaceVisibility.Private);
        evt.UpdatedBy.Should().Be(actor);
    }

    [Fact]
    public void ChangeVisibility_WhenSameValue_ShouldBeNoOp()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();

        space.ChangeVisibility(SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeVisibility_ArchivedSpace_ShouldThrow()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.ChangeVisibility(SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void ChangeType_ShouldSucceed_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var actor = Guid.NewGuid();

        space.ChangeType(SpaceType.Portfolio, actor, DateTimeOffset.UtcNow);

        space.SpaceType.Should().Be(SpaceType.Portfolio);
        space.DomainEvents.Should().ContainSingle(e => e is SpaceTypeChangedDomainEvent);
        var evt = (SpaceTypeChangedDomainEvent)space.DomainEvents.First();
        evt.OldType.Should().Be(SpaceType.Folder);
        evt.NewType.Should().Be(SpaceType.Portfolio);
        evt.UpdatedBy.Should().Be(actor);
    }

    [Fact]
    public void ChangeType_WhenSameValue_ShouldBeNoOp()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();

        space.ChangeType(SpaceType.Folder, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeType_ArchivedSpace_ShouldThrow()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.ChangeType(SpaceType.Portfolio, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Delete_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.IsDeleted.Should().BeTrue();
        space.DomainEvents.Should().Contain(e => e is SpaceDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();

        space.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        space.IsDeleted.Should().BeFalse();
        space.DomainEvents.Should().Contain(e => e is SpaceRestoredDomainEvent);
    }

    [Fact]
    public void Rename_OnDeletedSpace_ShouldThrow()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => space.Rename("Sales", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted and cannot be modified*");
    }

    [Fact]
    public void Rename_ArchivedSpace_ShouldNotMutateName()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Space", SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var originalName = space.Name;

        var act = () => space.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        space.Name.Should().Be(originalName);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDescription_ArchivedSpace_ShouldNotMutateDescription()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Space", SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow, description: "Original");
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var originalDescription = space.Description;

        var act = () => space.UpdateDescription("New description", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        space.Description.Should().Be(originalDescription);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeVisibility_ArchivedSpace_ShouldNotMutateVisibility()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Space", SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var originalVisibility = space.Visibility;

        var act = () => space.ChangeVisibility(SpaceVisibility.Workspace, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        space.Visibility.Should().Be(originalVisibility);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeType_ArchivedSpace_ShouldNotMutateType()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Space", SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        space.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var originalType = space.SpaceType;

        var act = () => space.ChangeType(SpaceType.Portfolio, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        space.SpaceType.Should().Be(originalType);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rename_EmptyActor_ShouldNotMutateName()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Space", SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var originalName = space.Name;

        var act = () => space.Rename("New Name", Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        space.Name.Should().Be(originalName);
        space.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_EmptyActor_ShouldNotMutateStatus()
    {
        var space = Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Space", SpaceVisibility.Private, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)space).ClearDomainEvents();
        var originalStatus = space.Status;

        var act = () => space.Archive(Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        space.Status.Should().Be(originalStatus);
        space.DomainEvents.Should().BeEmpty();
    }
}
