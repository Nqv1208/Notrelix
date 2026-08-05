using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateArchiveAtomicityTests
{
    private static PermissionTemplateDefinition ValidDefinition() =>
        PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

    [Fact]
    public void Archive_ShouldSetStatusArchived()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PermissionTemplateStatus.Archived);
    }

    [Fact]
    public void Archive_ShouldRaiseEventWithCorrectScope()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var template = PermissionTemplate.CreateWorkspace(accountId, workspaceId, "Template", ValidDefinition(), Guid.NewGuid(), now);

        template.Archive(updatedBy, now);

        var evt = template.DomainEvents.OfType<PermissionTemplateArchivedDomainEvent>().Single();
        evt.AccountId.Should().Be(accountId);
        evt.WorkspaceId.Should().Be(workspaceId);
        evt.ArchivedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = template.Version;

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldNotRaiseEvent()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)template).ClearDomainEvents();

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.DomainEvents.Should().NotContain(e => e is PermissionTemplateArchivedDomainEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldNotIncrementVersion()
    {
        var template = PermissionTemplate.CreateWorkspace(Guid.NewGuid(), Guid.NewGuid(), "Template", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = template.Version;

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Version.Should().Be(before);
    }

    [Fact]
    public void Archive_WhenSystem_ShouldNotChangeStatus()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        template.Status.Should().Be(PermissionTemplateStatus.Active);
    }
}
