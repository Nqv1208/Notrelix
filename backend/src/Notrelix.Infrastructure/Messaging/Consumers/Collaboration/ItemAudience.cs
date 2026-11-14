using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

/// <summary>
/// Who may be told about an Item: only people who can see its Board now. A notification carries the Item's
/// title or an excerpt and a link, so a recipient without Board access would learn what they may not open.
/// Fails closed: an Item that is gone, or a visibility read that finds no Board, tells nobody.
/// </summary>
public sealed class ItemAudience
{
    private readonly IWorkItemNotificationFacts _work;
    private readonly IResourceVisibility _visibility;

    public ItemAudience(IWorkItemNotificationFacts work, IResourceVisibility visibility)
    {
        _work = work;
        _visibility = visibility;
    }

    // shortcut: one visibility read per person (no batched read exists), fine for team-sized audiences.
    public async Task<IReadOnlyList<Guid>> WhoCanSeeAsync(
        Guid workspaceId, Guid itemId, IReadOnlyCollection<Guid> recipients, CancellationToken ct)
    {
        if (recipients.Count == 0) return [];
        var facts = await _work.FindAsync(workspaceId, itemId, null, ct);
        if (facts is null) return [];

        var allowed = new List<Guid>();
        foreach (var userId in recipients.Distinct())
        {
            if ((await _visibility.VisibleBoardIdsAsync(workspaceId, userId, ct)).Contains(facts.BoardId)) allowed.Add(userId);
        }
        return allowed;
    }
}
