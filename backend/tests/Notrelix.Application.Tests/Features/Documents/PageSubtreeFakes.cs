using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Documents;

/// <summary>In-memory page tree for the handlers that cascade over a page subtree.</summary>
internal sealed class InMemoryPageSubtreeStore : IPageSubtreeStore
{
    private readonly List<Page> _pages;

    public InMemoryPageSubtreeStore(params Page[] pages) => _pages = [.. pages];

    public Task LockTreeAsync(Guid workspaceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<Page?> FindAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken) =>
        Task.FromResult(_pages.FirstOrDefault(p => p.WorkspaceId == workspaceId && p.Id == pageId));

    public Task<IReadOnlyList<Page>> LoadDescendantsAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken)
    {
        var found = new List<Page>();
        var frontier = new List<Guid> { pageId };
        while (frontier.Count > 0)
        {
            var children = _pages.Where(p => !p.IsDeleted && p.ParentId is { } parent && frontier.Contains(parent)).ToList();
            found.AddRange(children);
            frontier = children.Select(c => c.Id).ToList();
        }
        return Task.FromResult<IReadOnlyList<Page>>(found);
    }

    public async Task<int> SubtreeDepthAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken)
    {
        var descendants = await LoadDescendantsAsync(workspaceId, pageId, cancellationToken);
        var level = new Dictionary<Guid, int> { [pageId] = 1 };
        foreach (var descendant in descendants)
            level[descendant.Id] = level.GetValueOrDefault(descendant.ParentId!.Value, 1) + 1;
        return level.Values.Max();
    }

    public async Task<int> DepthOfAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken) =>
        (await LoadAncestryAsync(workspaceId, pageId, cancellationToken)).Count;

    public Task<IReadOnlyDictionary<Guid, Guid?>> LoadAncestryAsync(Guid workspaceId, Guid pageId, CancellationToken cancellationToken)
    {
        var chain = new Dictionary<Guid, Guid?>();
        var current = _pages.FirstOrDefault(p => p.Id == pageId);
        while (current is not null && chain.TryAdd(current.Id, current.ParentId))
            current = current.ParentId is { } parent ? _pages.FirstOrDefault(p => p.Id == parent) : null;
        return Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(chain);
    }
}

/// <summary>Allows every page and records the ids the cascade asked about.</summary>
internal sealed class RecordingSubtreeAuthorizer : IPageSubtreeAuthorizer
{
    public List<Guid> Checked { get; } = [];

    public Task EnsureAllowedAsync<TRequest>(IEnumerable<Guid> pageIds, Func<Guid, TRequest> commandFor, CancellationToken cancellationToken)
        where TRequest : notnull
    {
        Checked.AddRange(pageIds);
        return Task.CompletedTask;
    }
}

internal static class PageRequestContext
{
    /// <summary>A request context for a user working in the workspace the page lives in.</summary>
    public static FakeCurrentRequestContext For(Page page)
    {
        var context = new FakeCurrentRequestContext().AsUser(Guid.CreateVersion7());
        context.Tenant.SetWorkspace(page.AccountId, page.WorkspaceId, context.UserId);
        return context;
    }
}
