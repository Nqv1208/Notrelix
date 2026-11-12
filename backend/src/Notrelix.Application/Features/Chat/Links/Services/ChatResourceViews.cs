using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Links.DTOs;
using Notrelix.Application.Features.Documents.Public.PageTitles;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.BoardTitles;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Links.Services;

/// <summary>
/// Chat's one answer to "what is this Board or Page called, for this person": the title only when the person can view the resource now,
/// otherwise null, the same answer whether it is hidden, trashed or gone (UC-CHT-H10, CHT-023). It uses the producing contexts' published
/// facts (the Governance collection visibility, the Page view decision, the title facts) and never another context's persistence. A
/// Team has no published visibility fact yet, so it is never viewable here (a recorded gap): a Team link cannot be set and one that
/// exists degrades to a generic chip.
/// </summary>
public sealed class ChatResourceViews(
    IResourceVisibility visibility,
    IPageViewAccess pageAccess,
    IPageTitleFacts pageTitles,
    IBoardTitleFacts boardTitles)
{
    /// <summary>The kinds a channel can be linked to now; a Team is a gap until Governance publishes its visibility.</summary>
    public static bool IsLinkable(string? kind) => kind is ChannelLinkKinds.Board or ChannelLinkKinds.Page;

    /// <summary>The title of one resource when the person may view it, else null.</summary>
    public async Task<string?> TitleAsync(Guid accountId, Guid workspaceId, Guid userId, string kind, Guid resourceId, CancellationToken ct)
    {
        var titles = await TitlesAsync(accountId, workspaceId, userId, [(kind, resourceId)], ct);
        return titles.TryGetValue((kind, resourceId), out var title) ? title : null;
    }

    /// <summary>The titles of the resources the person may view, keyed by kind and id. A resource they cannot view is absent.</summary>
    public async Task<IReadOnlyDictionary<(string Kind, Guid Id), string>> TitlesAsync(
        Guid accountId, Guid workspaceId, Guid userId, IReadOnlyCollection<(string Kind, Guid Id)> refs, CancellationToken ct)
    {
        var result = new Dictionary<(string, Guid), string>();

        var boardIds = refs.Where(r => r.Kind == ChannelLinkKinds.Board).Select(r => r.Id).Distinct().ToList();
        if (boardIds.Count > 0)
        {
            var visible = (await visibility.VisibleBoardIdsAsync(workspaceId, userId, ct)).ToHashSet();
            var allowed = boardIds.Where(visible.Contains).ToList();
            if (allowed.Count > 0)
            {
                foreach (var (id, title) in await boardTitles.TitlesAsync(workspaceId, allowed, ct))
                    result[(ChannelLinkKinds.Board, id)] = title;
            }
        }

        var pageIds = refs.Where(r => r.Kind == ChannelLinkKinds.Page).Select(r => r.Id).Distinct().ToList();
        if (pageIds.Count > 0)
        {
            var allowed = new List<Guid>();
            foreach (var id in pageIds)
            {
                if (await pageAccess.CanViewAsync(accountId, workspaceId, id, userId, ct))
                    allowed.Add(id);
            }

            if (allowed.Count > 0)
            {
                foreach (var (id, title) in await pageTitles.TitlesAsync(workspaceId, allowed, ct))
                    result[(ChannelLinkKinds.Page, id)] = title;
            }
        }

        return result;
    }

    /// <summary>Fills in the link of each channel as the viewer sees it (title or generic). Channels without a link are returned as they are.</summary>
    public async Task<IReadOnlyList<ChannelDto>> WithLinksAsync(
        IReadOnlyList<ChannelDto> channels, Guid accountId, Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var refs = channels.Where(c => c.Link is not null).Select(c => (c.Link!.Kind, c.Link.ResourceId)).Distinct().ToList();
        if (refs.Count == 0)
            return channels;

        var views = await ViewsAsync(accountId, workspaceId, userId, refs, ct);
        return channels.Select(c => c.Link is null ? c : c with { Link = views[(c.Link.Kind, c.Link.ResourceId)] }).ToList();
    }

    /// <summary>The links as one viewer sees them: the title where they can view the resource, a generic link otherwise.</summary>
    public async Task<IReadOnlyDictionary<(string Kind, Guid Id), ChannelLinkDto>> ViewsAsync(
        Guid accountId, Guid workspaceId, Guid userId, IReadOnlyCollection<(string Kind, Guid Id)> refs, CancellationToken ct)
    {
        var titles = await TitlesAsync(accountId, workspaceId, userId, refs, ct);
        return refs.Distinct().ToDictionary(
            r => r,
            r => titles.TryGetValue(r, out var title) ? new ChannelLinkDto(r.Kind, r.Id, true, title) : new ChannelLinkDto(r.Kind, r.Id, false, null));
    }
}
