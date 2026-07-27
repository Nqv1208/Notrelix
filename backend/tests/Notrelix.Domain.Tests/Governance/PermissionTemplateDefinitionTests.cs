using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateDefinitionTests
{
    [Fact]
    public void Create_ShouldSetSchemaVersion()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

        definition.SchemaVersion.Should().Be(1);
        definition.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void Create_EmptyEntries_ShouldThrow()
    {
        var act = () => PermissionTemplateDefinition.Create([]);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_DuplicateEntries_ShouldThrow()
    {
        var entry = PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow);

        var act = () => PermissionTemplateDefinition.Create([entry, entry]);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Entries_ShouldBeImmutable()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceType.Board, PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

        definition.Entries.Should().AllSatisfy(e =>
        {
            e.Resource.Should().Be(ResourceType.Board);
            e.Action.Should().Be(PermissionAction.ViewBoard);
            e.Effect.Should().Be(PermissionEffect.Allow);
        });
    }
}
