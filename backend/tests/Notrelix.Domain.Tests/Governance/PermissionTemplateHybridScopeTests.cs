using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateHybridScopeTests
{
    private static PermissionTemplateDefinition ValidDefinition() =>
        PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

    [Fact]
    public void SystemScope_ShouldHaveNullAccountAndWorkspaceId()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.AccountId.Should().BeNull();
        template.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public void WorkspaceScope_ShouldHaveNonEmptyAccountAndWorkspaceId()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var template = PermissionTemplate.CreateWorkspace(accountId, workspaceId, "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.AccountId.Should().Be(accountId);
        template.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Scope_ShouldNotChange_OnLifecycleMutation()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Scope.Should().Be(PermissionTemplateScope.Workspace);
        template.AccountId.Should().NotBeNull();
        template.WorkspaceId.Should().NotBeNull();
    }

    [Fact]
    public void SystemScope_ShouldNotAllowArchive()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void WorkspaceArchive_ShouldBeWorkspaceScoped()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var evt = template.DomainEvents.OfType<PermissionTemplateArchivedDomainEvent>().Single();
        evt.Should().BeAssignableTo<IWorkspaceScoped>();
    }

    [Fact]
    public void SystemCreate_ShouldBeGlobalEvent()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var evt = template.DomainEvents.OfType<SystemPermissionTemplateCreatedDomainEvent>().Single();
        evt.Should().BeAssignableTo<GlobalDomainEvent>();
        evt.Should().NotBeAssignableTo<IWorkspaceScoped>();
    }

    [Fact]
    public void WorkspaceCreate_ShouldBeWorkspaceScopedEvent()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var evt = template.DomainEvents.OfType<WorkspacePermissionTemplateCreatedDomainEvent>().Single();
        evt.Should().BeAssignableTo<IWorkspaceScoped>();
    }
}
