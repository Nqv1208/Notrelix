namespace Notrelix.Domain.Documents.Pages.Events;

/// <summary>A person a saved block newly mentions (not in the block's previous content).</summary>
public sealed record BlockMentionAdded(Guid BlockId, Guid UserId);

/// <summary>
/// One successful save of the blocks of a page. It carries only what a later reaction needs: who saved,
/// and the mentions the save added. It says nothing about the page's own version.
/// </summary>
[EventName("documents.page-blocks-saved")]
public sealed record PageBlocksSavedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PageId,
    string PageTitle,
    Guid SavedBy,
    IReadOnlyList<BlockMentionAdded> Mentions,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
