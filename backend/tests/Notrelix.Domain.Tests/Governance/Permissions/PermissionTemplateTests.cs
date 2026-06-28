using FluentAssertions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var permissions = JsonValue.EmptyObject();
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.Create("Admin Template", permissions, Guid.NewGuid(), now);

        template.Name.Should().Be("Admin Template");
        template.Status.Should().Be(PermissionTemplateStatus.Active);
        template.IsSystem.Should().BeFalse();
        template.DomainEvents.Should().ContainSingle(e => e is PermissionTemplateCreatedEvent);
    }

    [Fact]
    public void Create_WithWorkspace_ShouldSetWorkspaceId()
    {
        var workspaceId = Guid.NewGuid();
        var permissions = JsonValue.EmptyObject();
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.Create("Template", permissions, Guid.NewGuid(), now, workspaceId: workspaceId);

        template.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Create_AsSystemTemplate_ShouldSetIsSystem()
    {
        var permissions = JsonValue.EmptyObject();
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.Create("System Template", permissions, Guid.NewGuid(), now, isSystem: true);

        template.IsSystem.Should().BeTrue();
        template.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var permissions = JsonValue.EmptyObject();
        var act = () => PermissionTemplate.Create(null!, permissions, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullPermissions_ShouldThrow()
    {
        var act = () => PermissionTemplate.Create("Template", null!, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var permissions = JsonValue.EmptyObject();
        var template = PermissionTemplate.Create("  Template  ", permissions, Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Name.Should().Be("Template");
    }
}
