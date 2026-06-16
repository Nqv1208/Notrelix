using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Templates.Events;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Documents;

public class PageTemplateTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var pageSnapshot = JsonValue.EmptyObject();
        var blocksSnapshot = JsonValue.EmptyArray();
        var now = DateTimeOffset.UtcNow;

        var template = PageTemplate.Create("Meeting Notes", pageSnapshot, blocksSnapshot, now);

        template.Name.Should().Be("Meeting Notes");
        template.Status.Should().Be(PageTemplateStatus.Draft);
        template.DomainEvents.Should().ContainSingle(e => e is PageTemplateCreatedEvent);
    }

    [Fact]
    public void Create_WithWorkspace_ShouldSetWorkspaceId()
    {
        var workspaceId = Guid.NewGuid();
        var template = PageTemplate.Create("Notes", JsonValue.EmptyObject(), JsonValue.EmptyArray(), DateTimeOffset.UtcNow, workspaceId);

        template.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => PageTemplate.Create(null!, JsonValue.EmptyObject(), JsonValue.EmptyArray(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullPageSnapshot_ShouldThrow()
    {
        var act = () => PageTemplate.Create("Notes", null!, JsonValue.EmptyArray(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Publish_ShouldSetStatusToPublished_AndRaiseEvent()
    {
        var template = CreateTemplate();

        template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PageTemplateStatus.Published);
        template.DomainEvents.Should().ContainSingle(e => e is PageTemplatePublishedEvent);
    }

    [Fact]
    public void Publish_WhenAlreadyArchived_ShouldThrow()
    {
        var template = CreateTemplate();
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void Archive_ShouldSetStatusToArchived()
    {
        var template = CreateTemplate();

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PageTemplateStatus.Archived);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeNoOp()
    {
        var template = CreateTemplate();
        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PageTemplateStatus.Archived);
    }

    [Fact]
    public void Publish_ThenArchive_ShouldWork()
    {
        var template = CreateTemplate();
        template.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Status.Should().Be(PageTemplateStatus.Archived);
    }

    private static PageTemplate CreateTemplate()
    {
        return PageTemplate.Create("Template", JsonValue.EmptyObject(), JsonValue.EmptyArray(), DateTimeOffset.UtcNow);
    }
}
