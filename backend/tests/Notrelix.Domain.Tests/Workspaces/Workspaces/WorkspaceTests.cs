using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);

        workspace.Name.Should().Be("My Workspace");
        workspace.Slug.Should().Be("my-workspace");
        workspace.AccountId.Should().Be(AccountId);
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceCreatedDomainEvent);
    }

    [Fact]
    public void CreateWithOwner_ShouldCreateWorkspaceAndOwnerMember()
    {
        var result = WorkspaceFactory.CreateWithOwner(AccountId, OwnerId, "My Workspace", "my-workspace", Now);

        result.Workspace.Should().NotBeNull();
        result.Workspace.Name.Should().Be("My Workspace");
        result.Workspace.Slug.Should().Be("my-workspace");
        result.Workspace.AccountId.Should().Be(AccountId);

        result.OwnerMember.Should().NotBeNull();
        result.OwnerMember.WorkspaceId.Should().Be(result.Workspace.Id);
        result.OwnerMember.UserId.Should().Be(OwnerId);
        result.OwnerMember.Role.Should().Be(WorkspaceRole.Owner);
        result.OwnerMember.Status.Should().Be(WorkspaceMemberStatus.Active);
    }

    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var actor = Guid.NewGuid();

        workspace.Rename("New Name", actor, Now);

        workspace.Name.Should().Be("New Name");
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceRenamedDomainEvent);
    }

    [Fact]
    public void Rename_ArchivedWorkspace_ShouldThrow()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);

        var act = () => workspace.Rename("New Name", Guid.NewGuid(), Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename an archived workspace.");
    }

    [Fact]
    public void UpdateSettings_ShouldSucceed_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var settings = WorkspaceSettings.Create(allowPublicSharing: true, enforceMfa: true);
        var actor = Guid.NewGuid();

        workspace.UpdateSettings(settings, actor, Now);

        workspace.Settings.AllowPublicSharing.Should().BeTrue();
        workspace.Settings.EnforceMfa.Should().BeTrue();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceSettingsUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateSettings_WhenSameSettings_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        var settings = workspace.Settings;
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.UpdateSettings(settings, Guid.NewGuid(), Now);

        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateSettings_ArchivedWorkspace_ShouldThrow()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);

        var settings = WorkspaceSettings.Create(allowPublicSharing: true, enforceMfa: true);
        var act = () => workspace.UpdateSettings(settings, Guid.NewGuid(), Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot update settings of an archived workspace.");
    }

    [Fact]
    public void Delete_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var actor = Guid.NewGuid();

        workspace.Delete(actor, Now);

        workspace.IsDeleted.Should().BeTrue();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Delete(Guid.NewGuid(), Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        var actor = Guid.NewGuid();
        workspace.Restore(actor, Now);

        workspace.IsDeleted.Should().BeFalse();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceRestoredDomainEvent);
    }

    [Fact]
    public void Archive_ShouldSetStatusToArchived_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Archive(Guid.NewGuid(), Now);

        workspace.Status.Should().Be(WorkspaceStatus.Archived);
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceArchivedDomainEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Archive(Guid.NewGuid(), Now);

        workspace.Status.Should().Be(WorkspaceStatus.Archived);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var actor = Guid.NewGuid();

        workspace.Unarchive(actor, Now);

        workspace.Status.Should().Be(WorkspaceStatus.Active);
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceUnarchivedDomainEvent);
    }

    [Fact]
    public void Unarchive_WhenAlreadyActive_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.Unarchive(Guid.NewGuid(), Now);

        workspace.Status.Should().Be(WorkspaceStatus.Active);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_Deleted_ShouldThrow()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Delete(Guid.NewGuid(), Now);

        var act = () => workspace.Unarchive(Guid.NewGuid(), Now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void UpdateDescription_ShouldSucceed_AndRaiseEvent()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var actor = Guid.NewGuid();

        workspace.UpdateDescription("New description", actor, Now);

        workspace.Description.Should().Be("New description");
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceDescriptionUpdatedDomainEvent);
        var domainEvent = workspace.DomainEvents.OfType<WorkspaceDescriptionUpdatedDomainEvent>().Single();
        domainEvent.OldDescription.Should().BeNull();
        domainEvent.NewDescription.Should().Be("New description");
        domainEvent.WorkspaceId.Should().Be(workspace.Id);
        domainEvent.UpdatedBy.Should().Be(actor);
    }

    [Fact]
    public void UpdateDescription_ShouldClearDescription_WhenSetToNull()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now, description: "Initial description");
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var actor = Guid.NewGuid();

        workspace.UpdateDescription(null, actor, Now);

        workspace.Description.Should().BeNull();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceDescriptionUpdatedDomainEvent);
        var domainEvent = workspace.DomainEvents.OfType<WorkspaceDescriptionUpdatedDomainEvent>().Single();
        domainEvent.OldDescription.Should().Be("Initial description");
        domainEvent.NewDescription.Should().BeNull();
    }

    [Fact]
    public void UpdateDescription_WhenSameValue_ShouldBeNoOp()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now, description: "Same");
        ((IHasDomainEvents)workspace).ClearDomainEvents();

        workspace.UpdateDescription("Same", Guid.NewGuid(), Now);

        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDescription_ArchivedWorkspace_ShouldThrow()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);

        var act = () => workspace.UpdateDescription("New description", Guid.NewGuid(), Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot update description of an archived workspace.");
    }

    [Fact]
    public void Create_PersonalWorkspace_ShouldHaveAccountId()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "Personal", "personal", Now, isPersonal: true);

        workspace.AccountId.Should().Be(AccountId);
        workspace.IsPersonal.Should().BeTrue();
    }

    [Fact]
    public void Rename_ArchivedWorkspace_ShouldNotMutateName()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var originalName = workspace.Name;

        var act = () => workspace.Rename("New Name", Guid.NewGuid(), Now);

        act.Should().Throw<BusinessRuleException>();
        workspace.Name.Should().Be(originalName);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDescription_ArchivedWorkspace_ShouldNotMutateDescription()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now, description: "Original");
        workspace.Archive(Guid.NewGuid(), Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var originalDescription = workspace.Description;

        var act = () => workspace.UpdateDescription("New description", Guid.NewGuid(), Now);

        act.Should().Throw<BusinessRuleException>();
        workspace.Description.Should().Be(originalDescription);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateSettings_ArchivedWorkspace_ShouldNotMutateSettings()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        workspace.Archive(Guid.NewGuid(), Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var originalSettings = workspace.Settings;

        var settings = WorkspaceSettings.Create(allowPublicSharing: true);
        var act = () => workspace.UpdateSettings(settings, Guid.NewGuid(), Now);

        act.Should().Throw<BusinessRuleException>();
        workspace.Settings.Should().BeSameAs(originalSettings);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rename_EmptyActor_ShouldNotMutateName()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var originalName = workspace.Name;

        var act = () => workspace.Rename("New Name", Guid.Empty, Now);

        act.Should().Throw<BusinessRuleException>();
        workspace.Name.Should().Be(originalName);
        workspace.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_EmptyActor_ShouldNotMutateStatus()
    {
        var workspace = Workspace.Create(AccountId, OwnerId, "My Workspace", "my-workspace", Now);
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        var originalStatus = workspace.Status;

        var act = () => workspace.Archive(Guid.Empty, Now);

        act.Should().Throw<BusinessRuleException>();
        workspace.Status.Should().Be(originalStatus);
        workspace.DomainEvents.Should().BeEmpty();
    }
}
