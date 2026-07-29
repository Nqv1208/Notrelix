using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

[CoversAggregate(typeof(PermissionTemplate))]
public class PermissionTemplateLifecycleTests
{
    private static PermissionTemplateDefinition ValidDefinition() =>
        PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

    [Fact]
    public void CreateSystem_ShouldSetScopeToSystem()
    {
        var template = PermissionTemplate.CreateSystem("Admin Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Scope.Should().Be(PermissionTemplateScope.System);
        template.Status.Should().Be(PermissionTemplateStatus.Active);
        template.Name.Should().Be("Admin Template");
    }

    [Fact]
    public void CreateSystem_WithDescription_ShouldSetDescription()
    {
        var template = PermissionTemplate.CreateSystem("Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow, description: "A description");

        template.Description.Should().Be("A description");
    }

    [Fact]
    public void CreateSystem_WithTargetResourceType_ShouldSetTarget()
    {
        var template = PermissionTemplate.CreateSystem("Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow, targetResourceType: ResourceType.Board);

        template.TargetResourceType.Should().Be(ResourceType.Board);
    }

    [Fact]
    public void CreateSystem_ShouldRaiseEvent()
    {
        var template = PermissionTemplate.CreateSystem("Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.DomainEvents.Should().ContainSingle(e => e is PermissionTemplateCreatedDomainEvent);
    }

    [Fact]
    public void CreateSystem_WithNullName_ShouldThrow()
    {
        var act = () => PermissionTemplate.CreateSystem(null!, ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateSystem_WithNullDefinition_ShouldThrow()
    {
        var act = () => PermissionTemplate.CreateSystem("Template", null!, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void CreateWorkspace_ShouldSetScopeToWorkspace()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Team Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Scope.Should().Be(PermissionTemplateScope.Workspace);
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

    [CoversMutation(typeof(PermissionTemplate), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Archive_WhenWorkspace_ShouldSetArchivedAndRaiseEvent()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var template = PermissionTemplate.CreateWorkspace(accountId, workspaceId, "Template", ValidDefinition(), Guid.NewGuid(), now);

        template.Archive(updatedBy, now);

        template.Status.Should().Be(PermissionTemplateStatus.Archived);
        template.DomainEvents.Should().ContainSingle(e => e is PermissionTemplateArchivedDomainEvent);
        var evt = (PermissionTemplateArchivedDomainEvent)template.DomainEvents.Single(e => e is PermissionTemplateArchivedDomainEvent);
        evt.AccountId.Should().Be(accountId);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.TemplateId.Should().Be(template.Id);
        evt.ArchivedBy.Should().Be(updatedBy);
    }

    [CoversMutation(typeof(PermissionTemplate), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Archive_WhenSystem_ShouldThrow()
    {
        var template = PermissionTemplate.CreateSystem("System Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*System*");
    }

    [CoversMutation(typeof(PermissionTemplate), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeNoOp()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PermissionTemplateStatus.Archived);
    }

    [CoversMutation(typeof(PermissionTemplate), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var version = template.Version;

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Version.Should().Be(version + 1);
    }
}
