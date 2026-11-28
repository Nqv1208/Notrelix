using Notrelix.Application.Features.Documents.Realtime;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Infrastructure.Data.Interceptors;

/// <summary>
/// Announces a change of an open page to its readers (UC-COL-E01, UC-DCT-K01). Before a save, it finds every page
/// the save touches (the page row itself, one of its blocks, or a comment on it) and enrolls one
/// <see cref="PageRealtime.Changed"/> fact per page in the scope's collector. It is registered ahead of
/// <see cref="DomainEventInterceptor"/>, which writes the collector to the outbox in the same transaction, so a frame
/// goes out only for a committed change. Writes made with ExecuteUpdate or ExecuteDelete are not seen.
/// It depends only on services every host that registers persistence has (the worker has no execution context), so
/// the frame names no actor: the client rereads over REST and never needs one.
/// </summary>
public sealed class PageRealtimeInterceptor : SaveChangesInterceptor
{
    private const string PageKind = "documents.page";

    private readonly IIntegrationEventCollector _events;
    private readonly IDateTimeProvider _time;

    public PageRealtimeInterceptor(IIntegrationEventCollector events, IDateTimeProvider time)
    {
        _events = events;
        _time = time;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enroll(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enroll(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Enroll(DbContext? context)
    {
        if (context is null) return;
        var tracker = context.ChangeTracker;

        var pages = tracker.Entries<Page>()
            .Where(e => IsWritten(e.State))
            .Select(e => (e.Entity.AccountId, e.Entity.WorkspaceId, PageId: e.Entity.Id));
        var blocks = tracker.Entries<Block>()
            .Where(e => IsWritten(e.State))
            .Select(e => (e.Entity.AccountId, e.Entity.WorkspaceId, e.Entity.PageId));
        var comments = tracker.Entries<Comment>()
            .Where(e => IsWritten(e.State) && e.Entity.Target is { } target && target.Kind.Value == PageKind)
            .Select(e => (e.Entity.AccountId, e.Entity.WorkspaceId, PageId: e.Entity.Target.ResourceId));

        var changed = pages.Concat(blocks).Concat(comments)
            .Where(p => p.PageId != Guid.Empty)
            .Distinct()
            .ToList();
        if (changed.Count == 0) return;

        var now = _time.UtcNow;
        foreach (var (accountId, workspaceId, pageId) in changed)
        {
            _events.Add(PageRealtime.Changed(
                accountId, workspaceId, pageId, actorUserId: null, correlationId: null, causationId: null, now));
        }
    }

    private static bool IsWritten(EntityState state) =>
        state is EntityState.Added or EntityState.Modified or EntityState.Deleted;
}
