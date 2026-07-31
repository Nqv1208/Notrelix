using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.WorkManagement.Templates;

namespace Notrelix.Domain.Tests.WorkManagement.Templates;

[CoversAggregate(typeof(BoardTemplate))]
public class TemplateTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // ── BoardTemplate ─────────────────────────────────────────────────────

    [Fact]
    public void BoardTemplate_Create_ShouldSetAudit()
    {
        var template = BoardTemplate.Create("Template", JsonValue.EmptyObject(), Now);

        template.CreatedAt.Should().Be(Now);
        template.Name.Should().Be("Template");
        template.Status.Should().Be(TemplateStatus.Published);
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardTemplate_Restore_ShouldClearIsDeleted()
    {
        var template = BoardTemplate.Create("Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);
        template.IsDeleted.Should().BeTrue();

        template.Restore(Actor, Now);

        template.IsDeleted.Should().BeFalse();
        template.Status.Should().Be(TemplateStatus.Draft);
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Rename), MutationScenario.Invalid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardTemplate_Rename_ShouldThrow_WhenDeleted()
    {
        var template = BoardTemplate.Create("Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);

        var act = () => template.Rename("New Name", Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Draft), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardTemplate_Draft_ShouldThrow_WhenDeleted()
    {
        var template = BoardTemplate.Create("Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);

        var act = () => template.Draft(Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Publish), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardTemplate_Publish_ShouldThrow_WhenDeleted()
    {
        var template = BoardTemplate.Create("Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);

        var act = () => template.Publish(Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Archive), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void BoardTemplate_Archive_ShouldThrow_WhenDeleted()
    {
        var template = BoardTemplate.Create("Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);

        var act = () => template.Archive(Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }

    // ── ItemTemplate ──────────────────────────────────────────────────────

    [Fact]
    public void ItemTemplate_Create_ShouldSetAudit()
    {
        var template = ItemTemplate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item Template", JsonValue.EmptyObject(), Now);

        template.CreatedAt.Should().Be(Now);
        template.Name.Should().Be("Item Template");
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ItemTemplate_Restore_ShouldClearIsDeleted()
    {
        var template = ItemTemplate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);
        template.IsDeleted.Should().BeTrue();

        template.Restore(Actor, Now);

        template.IsDeleted.Should().BeFalse();
        template.Status.Should().Be(TemplateStatus.Draft);
    }

    [CoversMutation(typeof(BoardTemplate), nameof(BoardTemplate.Rename), MutationScenario.Invalid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void ItemTemplate_Rename_ShouldThrow_WhenDeleted()
    {
        var template = ItemTemplate.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Item Template", JsonValue.EmptyObject(), Now);
        template.Delete(Actor, Now);

        var act = () => template.Rename("New Name", Actor, Now);
        act.Should().Throw<BusinessRuleException>();
    }
}
