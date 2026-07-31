using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Domain.Tests.Governance;

[CoversAggregate(typeof(CustomRole))]
public class CustomRoleTests
{
    [CoversMutation(typeof(CustomRole), nameof(CustomRole.Rename), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(CustomRole), nameof(CustomRole.AssignToMember), MutationScenario.Event, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var role = CustomRole.Create(Guid.NewGuid(), workspaceId, "Project Manager", "Manages projects", createdBy, DateTimeOffset.UtcNow);

        role.Name.Should().Be("Project Manager");
        role.WorkspaceId.Should().Be(workspaceId);
        role.Status.Should().Be(CustomRoleStatus.Active);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleCreatedDomainEvent);
    }

    [CoversMutation(typeof(CustomRole), nameof(CustomRole.AddPermission), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(CustomRole), nameof(CustomRole.RemovePermission), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void AddPermission_ShouldAddToList_AndRaiseEvent()
    {
        var role = CustomRole.Create(Guid.NewGuid(), Guid.NewGuid(), "Role", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)role).ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        role.AddPermission("CreateBoard", updatedBy, DateTimeOffset.UtcNow);

        role.Permissions.Should().HaveCount(1);
        role.Permissions.First().Action.Should().Be("CreateBoard");
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleUpdatedDomainEvent);
    }

    [CoversMutation(typeof(CustomRole), nameof(CustomRole.RevokeFromMember), MutationScenario.Event, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(CustomRole), nameof(CustomRole.RemovePermission), MutationScenario.Event, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RemovePermission_ShouldRemoveFromList_AndRaiseEvent()
    {
        var role = CustomRole.Create(Guid.NewGuid(), Guid.NewGuid(), "Role", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        role.AddPermission("CreateBoard", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)role).ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        role.RemovePermission("CreateBoard", updatedBy, DateTimeOffset.UtcNow);

        role.Permissions.Should().BeEmpty();
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleUpdatedDomainEvent);
    }

    [CoversMutation(typeof(CustomRole), nameof(CustomRole.Archive), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldSetStatusAndRaiseEvent()
    {
        var role = CustomRole.Create(Guid.NewGuid(), Guid.NewGuid(), "Role", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)role).ClearDomainEvents();

        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        role.Status.Should().Be(CustomRoleStatus.Archived);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleArchivedDomainEvent);
    }

    [CoversMutation(typeof(CustomRole), nameof(CustomRole.Activate), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldRestoreStatusAndRaiseEvent()
    {
        var role = CustomRole.Create(Guid.NewGuid(), Guid.NewGuid(), "Role", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        role.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)role).ClearDomainEvents();

        role.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);

        role.Status.Should().Be(CustomRoleStatus.Active);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleActivatedDomainEvent);
    }
}
