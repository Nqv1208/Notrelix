using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

[CoversAggregate(typeof(PermissionTemplate))]
public class PermissionTemplateSystemScopeTests
{
    private static PermissionTemplateDefinition ValidDefinition() =>
        PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

    [Fact]
    public void CreateSystem_ShouldHaveNullAccountId()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.AccountId.Should().BeNull();
    }

    [Fact]
    public void CreateSystem_ShouldHaveNullWorkspaceId()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public void CreateSystem_ShouldSetScopeSystem()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Scope.Should().Be(PermissionTemplateScope.System);
    }

    [Fact]
    public void CreateSystem_Archive_ShouldThrow()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*System templates cannot be modified*");
    }

    [Fact]
    public void CreateSystem_Archive_ShouldNotRaiseEvent()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        template.DomainEvents.Should().NotContain(e => e is PermissionTemplateArchivedDomainEvent);
    }

    [Fact]
    public void CreateSystem_ShouldSetStatusActive()
    {
        var template = PermissionTemplate.CreateSystem("System", ValidDefinition(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PermissionTemplateStatus.Active);
    }
}
