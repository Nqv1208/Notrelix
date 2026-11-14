using FluentAssertions;
using Notrelix.Domain.WorkManagement.Templates;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardTemplateTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSucceedInItsWorkspace()
    {
        var template = BoardTemplate.Create(AccountId, WorkspaceId, "Sprint", "Two-week sprint", JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

        template.AccountId.Should().Be(AccountId);
        template.WorkspaceId.Should().Be(WorkspaceId);
        template.Name.Should().Be("Sprint");
        template.Description.Should().Be("Two-week sprint");
        template.Status.Should().Be(TemplateStatus.Draft, "a saved template is published by a workspace manager");
        template.DomainEvents.Should().ContainSingle(e => e is BoardTemplateCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithoutWorkspace_ShouldThrow()
    {
        var act = () => BoardTemplate.Create(AccountId, Guid.Empty, "Sprint", null, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => BoardTemplate.Create(AccountId, WorkspaceId, null!, null, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullStructure_ShouldThrow()
    {
        var act = () => BoardTemplate.Create(AccountId, WorkspaceId, "Sprint", null, null!, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldTrimNameAndDropBlankDescription()
    {
        var template = BoardTemplate.Create(AccountId, WorkspaceId, "  Sprint  ", "  ", JsonValue.EmptyObject(), DateTimeOffset.UtcNow);
        template.Name.Should().Be("Sprint");
        template.Description.Should().BeNull();
    }

    private static BoardTemplate Saved() =>
        BoardTemplate.Create(AccountId, WorkspaceId, "Sprint", null, JsonValue.EmptyObject(), DateTimeOffset.UtcNow);

    [Fact]
    public void Publish_ADraft_PublishesAndBumpsTheVersion_AndPublishingAgainIsANoOp()
    {
        var template = Saved();
        var actor = Guid.NewGuid();

        template.Publish(actor, DateTimeOffset.UtcNow);
        var version = template.Version;
        var events = template.DomainEvents.Count;
        template.Publish(actor, DateTimeOffset.UtcNow);

        template.Status.Should().Be(TemplateStatus.Published);
        template.Version.Should().Be(version, "publishing a published template changes nothing");
        template.DomainEvents.Should().HaveCount(events);
    }

    [Fact]
    public void Archive_APublishedTemplate_ArchivesIt_AndArchivingAgainIsANoOp_AndAnArchivedOneCannotBePublished()
    {
        var template = Saved();
        var actor = Guid.NewGuid();
        template.Publish(actor, DateTimeOffset.UtcNow);

        template.Archive(actor, DateTimeOffset.UtcNow);
        var version = template.Version;
        template.Archive(actor, DateTimeOffset.UtcNow);
        var publishArchived = () => template.Publish(actor, DateTimeOffset.UtcNow);

        template.Status.Should().Be(TemplateStatus.Archived);
        template.Version.Should().Be(version, "archiving an archived template changes nothing");
        publishArchived.Should().Throw<BusinessRuleException>("an archived template is restored first");
        template.Status.Should().Be(TemplateStatus.Archived);
        template.Version.Should().Be(version, "the refused publish left no change");
    }

    [Fact]
    public void Delete_ATemplate_DeletesIt_AndItCannotBePublishedAfterwards()
    {
        var template = Saved();
        var actor = Guid.NewGuid();

        template.Delete(actor, DateTimeOffset.UtcNow);
        var publishDeleted = () => template.Publish(actor, DateTimeOffset.UtcNow);

        template.IsDeleted.Should().BeTrue();
        publishDeleted.Should().Throw<BusinessRuleException>();
    }
}
