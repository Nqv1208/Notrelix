using FluentAssertions;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Roles;
using Xunit;

namespace Notrelix.Domain.Tests.Governance;

public class CustomRoleTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        
        var role = CustomRole.Create(workspaceId, "Project Manager", "Manages projects", createdBy);

        role.Name.Should().Be("Project Manager");
        role.WorkspaceId.Should().Be(workspaceId);
        role.Status.Should().Be(CustomRoleStatus.Active);
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleCreatedEvent);
    }

    [Fact]
    public void AddPermission_ShouldAddToList_AndRaiseEvent()
    {
        var role = CustomRole.Create(Guid.NewGuid(), "Role", null, Guid.NewGuid());
        role.ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        role.AddPermission("CreateBoard", updatedBy);

        role.Permissions.Should().HaveCount(1);
        role.Permissions.First().Action.Should().Be("CreateBoard");
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleUpdatedEvent);
    }

    [Fact]
    public void RemovePermission_ShouldRemoveFromList_AndRaiseEvent()
    {
        var role = CustomRole.Create(Guid.NewGuid(), "Role", null, Guid.NewGuid());
        role.AddPermission("CreateBoard", Guid.NewGuid());
        role.ClearDomainEvents();

        var updatedBy = Guid.NewGuid();
        role.RemovePermission("CreateBoard", updatedBy);

        role.Permissions.Should().BeEmpty();
        role.DomainEvents.Should().ContainSingle(e => e is CustomRoleUpdatedEvent);
    }
}
