using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateWorkspaceScopeTests
{
    private static PermissionTemplateDefinition ValidDefinition() =>
        PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

    [Fact]
    public void CreateWorkspace_ShouldStoreAccountId()
    {
        var accountId = Guid.NewGuid();

        var template = PermissionTemplate.CreateWorkspace(accountId, Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.AccountId.Should().Be(accountId);
    }

    [Fact]
    public void CreateWorkspace_ShouldStoreWorkspaceId()
    {
        var workspaceId = Guid.NewGuid();

        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), workspaceId, "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void CreateWorkspace_ShouldSetScopeWorkspace()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Scope.Should().Be(PermissionTemplateScope.Workspace);
    }

    [Fact]
    public void CreateWorkspace_ShouldSetStatusActive()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PermissionTemplateStatus.Active);
    }

    [Fact]
    public void CreateWorkspace_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => PermissionTemplate.CreateWorkspace(Guid.Empty, Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateWorkspace_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.Empty, "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }
}
