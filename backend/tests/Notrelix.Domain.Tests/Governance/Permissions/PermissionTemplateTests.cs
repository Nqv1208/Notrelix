using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateTests
{
    [Fact]
    public void CreateSystem_ShouldSucceed()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.CreateSystem("Admin Template", definition, Guid.NewGuid(), now);

        template.Name.Should().Be("Admin Template");
        template.Status.Should().Be(PermissionTemplateStatus.Active);
        template.Scope.Should().Be(PermissionTemplateScope.System);
        template.WorkspaceId.Should().BeNull();
        template.DomainEvents.Should().ContainSingle(e => e is PermissionTemplateCreatedDomainEvent);
    }

    [Fact]
    public void CreateWorkspace_ShouldSetWorkspaceId()
    {
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.CreateWorkspace(accountId, workspaceId, "Template", definition, Guid.NewGuid(), now);

        template.WorkspaceId.Should().Be(workspaceId);
        template.Scope.Should().Be(PermissionTemplateScope.Workspace);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);
        var act = () => PermissionTemplate.CreateSystem(null!, definition, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);
        var template = PermissionTemplate.CreateSystem("  Template  ", definition, Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Name.Should().Be("Template");
    }

    [Fact]
    public void Archive_SystemTemplate_ShouldThrow()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);
        var template = PermissionTemplate.CreateSystem("System", definition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*System*");
    }

    [Fact]
    public void Archive_WorkspaceTemplate_ShouldSucceed()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", definition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PermissionTemplateStatus.Archived);
    }
}
