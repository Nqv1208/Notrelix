using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateEventTests
{
    private static PermissionTemplateDefinition ValidDefinition() =>
        PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

    [Fact]
    public void CreateSystem_ShouldEmitCreatedEvent()
    {
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), createdBy, now);

        var evt = template.DomainEvents.OfType<SystemPermissionTemplateCreatedDomainEvent>().Single();
        evt.TemplateId.Should().Be(template.Id);
        evt.Name.Should().Be("System");
        evt.CreatedBy.Should().Be(createdBy);
        evt.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void CreateWorkspace_ShouldEmitCreatedEvent()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var template = PermissionTemplate.CreateWorkspace(accountId, workspaceId, "Workspace Template", ValidDefinition(), createdBy, now);

        var evt = template.DomainEvents.OfType<WorkspacePermissionTemplateCreatedDomainEvent>().Single();
        evt.AccountId.Should().Be(accountId);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.TemplateId.Should().Be(template.Id);
        evt.Name.Should().Be("Workspace Template");
        evt.CreatedBy.Should().Be(createdBy);
        evt.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void Archive_ShouldEmitArchivedEvent()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var archivedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var template = PermissionTemplate.CreateWorkspace(accountId, workspaceId, "Template", ValidDefinition(), Guid.NewGuid(), now);

        template.Archive(archivedBy, now);

        var evt = template.DomainEvents.OfType<PermissionTemplateArchivedDomainEvent>().Single();
        evt.TemplateId.Should().Be(template.Id);
        evt.AccountId.Should().Be(accountId);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.ArchivedBy.Should().Be(archivedBy);
        evt.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void CreateSystem_Event_ShouldBeGlobal()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var evt = template.DomainEvents.OfType<SystemPermissionTemplateCreatedDomainEvent>().Single();
        evt.Should().BeAssignableTo<GlobalDomainEvent>();
    }

    [Fact]
    public void CreateWorkspace_Event_ShouldBeWorkspaceScoped()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var evt = template.DomainEvents.OfType<WorkspacePermissionTemplateCreatedDomainEvent>().Single();
        evt.Should().BeAssignableTo<IWorkspaceScoped>();
    }

    [Fact]
    public void ArchivedEvent_ShouldBeWorkspaceScoped()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var evt = template.DomainEvents.OfType<PermissionTemplateArchivedDomainEvent>().Single();
        evt.Should().BeAssignableTo<IWorkspaceScoped>();
    }
}
