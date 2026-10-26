using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Search;
using Notrelix.Application.Features.Documents.Public.PageSearch;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.CommandSearch;

namespace Notrelix.Application.Features.Workspaces.Search.Queries.CommandSearch;

/// <summary>How a result matched: <c>title</c> (its name or key) or <c>child</c> (a sub-item's name, in <c>Snippet</c>).</summary>
public sealed record CommandSearchMatchDto(string Kind, string? Snippet);

/// <param name="AssigneeName">The first person in charge when it is someone else; null otherwise.</param>
/// <param name="Mine">Assigned to the caller.</param>
/// <param name="StatusCategory"><c>todo</c>, <c>in_progress</c>, <c>review</c> or <c>done</c>.</param>
public sealed record CommandSearchWorkDto(
    Guid Id,
    string? Key,
    string Title,
    Guid BoardId,
    string BoardName,
    string? AssigneeName,
    bool Mine,
    string? StatusName,
    string? StatusCategory,
    DateTimeOffset? DueAt,
    CommandSearchMatchDto Match);

/// <param name="SpaceId">The Space the Page is placed in, only when the caller can see that Space.</param>
/// <param name="Location">The readable ancestor Pages, outermost first, joined by " / "; null for a root Page.</param>
public sealed record CommandSearchDocDto(
    Guid Id,
    string Title,
    Guid? SpaceId,
    string? Location,
    DateTimeOffset? EditedAt,
    string? EditedByName,
    CommandSearchMatchDto Match);

/// <param name="SpaceId">The Space the Board is placed in, only when the caller can see that Space.</param>
/// <param name="MatchedItems">Readable matching Items of the Board.</param>
public sealed record CommandSearchBoardDto(Guid Id, string Name, Guid? SpaceId, int MatchedItems);

public sealed record CommandSearchDto(
    IReadOnlyList<CommandSearchWorkDto> Work,
    IReadOnlyList<CommandSearchDocDto> Docs,
    IReadOnlyList<CommandSearchBoardDto> Boards);

/// <summary>
/// The command palette's search (ADR-010): one typed text over the Items and Boards the caller may read (Work
/// Management) and the Pages the caller may read (Documents), within one Workspace. A pure query: it stores
/// nothing and decides no visibility. Boards, Pages and Spaces enter only through the Governance collection
/// visibility sets (GOV-029) and the producers' published search facts, so a name, key or count of something the
/// caller cannot open is never selected, returned or counted. Matching ignores case and marks ("hoa don" finds
/// "hoá đơn"); text shorter than two characters answers empty. The typed text is not logged or stored.
/// </summary>
public record CommandSearchQuery(Guid WorkspaceId, string? Text)
    : IQuery<Result<CommandSearchDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CommandSearchQueryHandler : IRequestHandler<CommandSearchQuery, Result<CommandSearchDto>>
{
    public const int WorkLimit = 6;
    public const int DocLimit = 5;
    public const int BoardLimit = 5;
    private const string Separator = " / ";

    private readonly IResourceVisibility _visibility;
    private readonly IWorkSearchFacts _work;
    private readonly IPageSearchFacts _pages;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;

    public CommandSearchQueryHandler(
        IResourceVisibility visibility,
        IWorkSearchFacts work,
        IPageSearchFacts pages,
        IActorLookupService actors,
        ICurrentRequestContext requestContext)
    {
        _visibility = visibility;
        _work = work;
        _pages = pages;
        _actors = actors;
        _requestContext = requestContext;
    }

    public async Task<Result<CommandSearchDto>> Handle(CommandSearchQuery request, CancellationToken ct)
    {
        var text = SearchTextFolding.Clean(request.Text);
        if (SearchTextFolding.Fold(text).Length < SearchTextFolding.MinLength)
            return Result<CommandSearchDto>.Success(Empty());

        var workspaceId = request.WorkspaceId;
        var userId = _requestContext.UserId;

        var readableBoards = await _visibility.VisibleBoardIdsAsync(workspaceId, userId, ct);
        var readablePages = await _visibility.VisiblePageIdsAsync(workspaceId, userId, ct);
        var visibleSpaces = (await _visibility.VisibleSpaceIdsAsync(workspaceId, userId, ct)).ToHashSet();

        var work = readableBoards.Count == 0
            ? WorkSearchResult.Empty
            : await _work.SearchAsync(workspaceId, userId, readableBoards, text, WorkLimit, BoardLimit, ct);
        var pages = readablePages.Count == 0
            ? []
            : await _pages.SearchAsync(workspaceId, readablePages, text, DocLimit, ct);

        var nameIds = work.Items.Where(i => i.OtherAssigneeUserId.HasValue).Select(i => i.OtherAssigneeUserId!.Value)
            .Concat(pages.Where(p => p.EditorUserId.HasValue).Select(p => p.EditorUserId!.Value))
            .Distinct()
            .ToList();
        var names = nameIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _actors.FindManyAsync(nameIds, ct)).ToDictionary(a => a.UserId, a => a.Name);

        Guid? Disclosed(Guid? spaceId) => spaceId is { } id && visibleSpaces.Contains(id) ? id : null;

        return Result<CommandSearchDto>.Success(new CommandSearchDto(
            work.Items.Select(i => new CommandSearchWorkDto(
                i.Id,
                i.Key,
                i.Name,
                i.BoardId,
                i.BoardName,
                i.OtherAssigneeUserId is { } assignee ? names.GetValueOrDefault(assignee) : null,
                i.Mine,
                i.StatusName,
                i.StatusCategory,
                i.DueAt,
                i.ChildMatch is null ? new CommandSearchMatchDto("title", null) : new CommandSearchMatchDto("child", i.ChildMatch))).ToList(),
            pages.Select(p => new CommandSearchDocDto(
                p.Id,
                p.Title,
                Disclosed(p.SpaceId),
                p.Ancestors.Count == 0 ? null : string.Join(Separator, p.Ancestors),
                p.UpdatedAt,
                p.EditorUserId is { } editor ? names.GetValueOrDefault(editor) : null,
                new CommandSearchMatchDto("title", null))).ToList(),
            work.Boards.Select(b => new CommandSearchBoardDto(b.Id, b.Name, Disclosed(b.SpaceId), b.MatchedItems)).ToList()));
    }

    private static CommandSearchDto Empty() => new([], [], []);
}

public class CommandSearchQueryValidator : AbstractValidator<CommandSearchQuery>
{
    public CommandSearchQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
