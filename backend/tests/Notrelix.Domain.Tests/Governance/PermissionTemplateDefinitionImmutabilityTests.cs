using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionTemplateDefinitionImmutabilityTests
{
    [Fact]
    public void Create_ShouldCopyEntries()
    {
        var entries = new List<PermissionTemplateEntry>
        {
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow)
        };

        var definition = PermissionTemplateDefinition.Create(entries);

        entries.Add(PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ManageBoard, PermissionEffect.Allow));
        definition.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void Create_WithSourceList_ShouldNotAffectDefinition()
    {
        var source = new List<PermissionTemplateEntry>
        {
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow),
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ManageBoard, PermissionEffect.Allow)
        };

        var definition = PermissionTemplateDefinition.Create(source);
        source.Clear();

        definition.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithNullEntry_ShouldThrow()
    {
        var entries = new List<PermissionTemplateEntry?>
        {
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow),
            null!
        };

        var act = () => PermissionTemplateDefinition.Create(entries!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SchemaVersion_ShouldBeFixed()
    {
        var definition = PermissionTemplateDefinition.Create(
        [
            PermissionTemplateEntry.Create(ResourceKind.Create("work-management.board"), PermissionAction.ViewBoard, PermissionEffect.Allow)
        ]);

        definition.SchemaVersion.Should().Be(1);
    }
}
