using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Workspaces.Events;
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
    public void CreateWithOwner_ShouldCreateWorkspaceAndOwnerMember()
    {
        var ownerId = Guid.NewGuid();
        var result = WorkspaceFactory.CreateWithOwner(ownerId, "My Workspace", "my-workspace", DateTimeOffset.UtcNow);

        result.Workspace.Should().NotBeNull();
        result.Workspace.Name.Should().Be("My Workspace");
        result.Workspace.Slug.Should().Be("my-workspace");

        result.OwnerMember.Should().NotBeNull();
        result.OwnerMember.WorkspaceId.Should().Be(result.Workspace.Id);
        result.OwnerMember.UserId.Should().Be(ownerId);
        result.OwnerMember.Role.Should().Be(WorkspaceRole.Owner);
        result.OwnerMember.Status.Should().Be(WorkspaceMemberStatus.Active);
    }

    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        workspace.Rename("New Name", actor, now);

        workspace.Name.Should().Be("New Name");
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceRenamedEvent);
    }

    [Fact]
    public void Rename_ArchivedWorkspace_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => workspace.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename an archived workspace.");
    }

    [Fact]
    public void UpdateSettings_ShouldSucceed()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        var settings = WorkspaceSettings.Create(allowPublicSharing: true, enforceMfa: true);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        workspace.UpdateSettings(settings, actor, now);

        workspace.Settings.AllowPublicSharing.Should().BeTrue();
        workspace.Settings.EnforceMfa.Should().BeTrue();
    }

    [Fact]
    public void UpdateSettings_ArchivedWorkspace_ShouldThrow()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var settings = WorkspaceSettings.Create(allowPublicSharing: true, enforceMfa: true);
        var act = () => workspace.UpdateSettings(settings, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot update settings of an archived workspace.");
    }

    [Fact]
    public void SoftDelete_ShouldSetStatusToSoftDeleted_AndRaiseEvent()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.ClearDomainEvents();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        workspace.SoftDelete(actor, now);

        workspace.Status.Should().Be(WorkspaceStatus.SoftDeleted);
        workspace.IsDeleted.Should().BeTrue();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceSoftDeletedEvent);
    }

    [Fact]
    public void Restore_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", "my-workspace", DateTimeOffset.UtcNow);
        workspace.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        workspace.ClearDomainEvents();

        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        workspace.Restore(actor, now);

        workspace.Status.Should().Be(WorkspaceStatus.Active);
        workspace.IsDeleted.Should().BeFalse();
        workspace.DomainEvents.Should().ContainSingle(e => e is WorkspaceRestoredEvent);
    }
}
