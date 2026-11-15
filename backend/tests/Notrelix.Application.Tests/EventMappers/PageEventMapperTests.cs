using Notrelix.Domain.Common;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Application.EventMappers.Documents;
using Notrelix.Application.Events.Documents;

namespace Notrelix.Application.Tests.EventMappers;

/// <summary>Page lifecycle facts reach the outbox as versioned integration events that carry facts, not trees.</summary>
public class PageEventMapperTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Workspace = Guid.NewGuid();
    private static readonly Guid PageId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset At = new(2026, 11, 15, 3, 0, 0, TimeSpan.Zero);
    private readonly IIntegrationEventMapper _mapper = new PageEventMapper();

    private IntegrationEvent Single(IDomainEvent e) => (IntegrationEvent)_mapper.Map(e).Single().IntegrationEvent;

    [Fact]
    public void Rename_CarriesOldAndNewTitleAndTheActor()
    {
        var e = (PageRenamedIntegrationEvent)Single(new PageRenamedDomainEvent(Account, Workspace, PageId, "Old", "New", Actor, At));
        e.OldTitle.Should().Be("Old");
        e.NewTitle.Should().Be("New");
        e.ActorUserId.Should().Be(Actor);
        e.PageId.Should().Be(PageId);
        e.WorkspaceId.Should().Be(Workspace);
        e.OccurredAt.Should().Be(At);
        e.MessageName.Should().Be("page.renamed");
    }

    [Fact]
    public void Move_CarriesBothParents_AndSpaceChangeBothSpaces()
    {
        var old = Guid.NewGuid();
        var next = Guid.NewGuid();
        var moved = (PageMovedIntegrationEvent)Single(new PageMovedDomainEvent(Account, Workspace, PageId, old, next, Actor, At));
        moved.OldParentId.Should().Be(old);
        moved.NewParentId.Should().Be(next);
        var space = (PageSpaceChangedIntegrationEvent)Single(new PageSpaceChangedDomainEvent(Account, Workspace, PageId, old, null, Actor, At));
        space.OldSpaceId.Should().Be(old);
        space.NewSpaceId.Should().BeNull();
    }

    [Fact]
    public void RestoreAndDelete_NameThePageAndActor()
    {
        var restored = Single(new PageRestoredDomainEvent(Account, Workspace, PageId, Actor, At));
        var deleted = Single(new PageDeletedDomainEvent(Account, Workspace, PageId, Actor, At));
        restored.MessageName.Should().Be("page.restored");
        deleted.MessageName.Should().Be("page.deleted");
        restored.ActorUserId.Should().Be(Actor);
        deleted.ActorUserId.Should().Be(Actor);
    }

    [Fact]
    public void EachMapping_GetsItsOwnEventId_AndCorrelatesToTheDomainEvent()
    {
        var domain = new PageRestoredDomainEvent(Account, Workspace, PageId, Actor, At);
        var a = Single(domain);
        var b = Single(domain);
        a.EventId.Should().NotBe(b.EventId);
        a.CorrelationId.Should().Be(domain.EventId);
    }
}
