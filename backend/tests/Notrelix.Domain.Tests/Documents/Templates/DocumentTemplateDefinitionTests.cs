using FluentAssertions;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Documents.Templates;

public class DocumentTemplateDefinitionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Name.Should().Be("Template");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => PageTemplate.Create("", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldThrow()
    {
        var act = () => PageTemplate.Create("   ", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldSetDraftStatus()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Status.Should().Be(PageTemplateStatus.Draft);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var template = PageTemplate.Create("  Template  ", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Name.Should().Be("Template");
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Publish), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Publish_ShouldChangeStatus()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Status.Should().Be(PageTemplateStatus.Published);
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Publish), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Publish_ShouldIncrementVersion()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        var before = template.Version;
        template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Publish), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Publish), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Publish_AlreadyPublished_ShouldBeIdempotent()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = template.Version;
        template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Version.Should().Be(before);
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Archive), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldSetArchivedStatus()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Status.Should().Be(PageTemplateStatus.Archived);
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Archive), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_ShouldRaiseEvent()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.DomainEvents.OfType<PageTemplateArchivedDomainEvent>().Should().HaveCount(1);
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Archive), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Archive), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Archive_AlreadyArchived_ShouldBeIdempotent()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = template.Version;
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        template.Version.Should().Be(before);
    }

    [CoversMutation(typeof(PageTemplate), nameof(PageTemplate.Publish), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Publish_AfterArchive_ShouldThrow()
    {
        var template = PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var act = () => template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(DocumentRuleCodes.Documents_PageTemplate_CannotPublishArchived);
    }
}
