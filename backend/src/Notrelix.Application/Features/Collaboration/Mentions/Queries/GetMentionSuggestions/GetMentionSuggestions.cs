using System.Globalization;
using System.Text;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Mentions.DTOs;
using global::Notrelix.Application.Features.Documents.Public.PageViewAccess;
using global::Notrelix.Application.Features.Governance.Public.Visibility;
using global::Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;
using global::Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Collaboration.Mentions.Queries.GetMentionSuggestions;

/// <param name="Query">Typed text after @; matched against the start of any word of the display name.</param>
/// <param name="Limit">Absent or below 1 reads the default; above the maximum reads the maximum.</param>
public record GetMentionSuggestionsQuery(ResourceKind ResourceKind, Guid ResourceId, string? Query = null, int? Limit = null)
    : IQuery<Result<List<MentionSuggestionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public const int DefaultLimit = 8;
    public const int MaxLimit = 20;
    public const int MaxQueryLength = 64;

    private const string BoardItemKind = "work-management.board-item";
    private const string PageKind = "documents.page";

    public static GetMentionSuggestionsQuery ForBoardItem(Guid boardItemId, string? query, int? limit)
        => new(ResourceKind.Create(BoardItemKind), boardItemId, query, limit);

    public static GetMentionSuggestionsQuery ForPage(Guid pageId, string? query, int? limit)
        => new(ResourceKind.Create(PageKind), pageId, query, limit);

    // Same view action the target's comments are read under, so a caller who cannot open the target gets the pipeline's refusal.
    public PermissionAction Action => ResourceKind.Value == PageKind ? PermissionAction.ViewPage : PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public sealed class GetMentionSuggestionsQueryValidator : AbstractValidator<GetMentionSuggestionsQuery>
{
    public GetMentionSuggestionsQueryValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Query).MaximumLength(GetMentionSuggestionsQuery.MaxQueryLength);
    }
}

public sealed class GetMentionSuggestionsQueryHandler : IRequestHandler<GetMentionSuggestionsQuery, Result<List<MentionSuggestionDto>>>
{
    // shortcut: at most this many people are view-checked per request (one read each, no batched read exists);
    // a workspace where the first 200 by name cannot view the target suggests fewer. Upgrade to a batched audience read.
    public const int MaxViewChecks = 200;

    private readonly IWorkspaceMemberFacts _members;
    private readonly IActorLookupService _actors;
    private readonly IPageViewAccess _pages;
    private readonly IWorkItemNotificationFacts _items;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentUser _currentUser;
    private readonly IExecutionContextReader _executionContext;

    public GetMentionSuggestionsQueryHandler(
        IWorkspaceMemberFacts members,
        IActorLookupService actors,
        IPageViewAccess pages,
        IWorkItemNotificationFacts items,
        IResourceVisibility visibility,
        ICurrentUser currentUser,
        IExecutionContextReader executionContext)
    {
        _members = members;
        _actors = actors;
        _pages = pages;
        _items = items;
        _visibility = visibility;
        _currentUser = currentUser;
        _executionContext = executionContext;
    }

    public async Task<Result<List<MentionSuggestionDto>>> Handle(GetMentionSuggestionsQuery request, CancellationToken ct)
    {
        List<MentionSuggestionDto> none = [];

        // Fail closed: without the request's account and workspace nobody is confirmed.
        if (_executionContext.Snapshot is not { AccountId: { } accountId, WorkspaceId: { } workspaceId })
            return Result<List<MentionSuggestionDto>>.Success(none);

        // Active, non-Guest members only. A caller outside that list (a Guest) must not enumerate people.
        var colleagues = await _members.ListActiveColleagueIdsAsync(workspaceId, ct);
        var me = _currentUser.UserId;
        if (!colleagues.Contains(me))
            return Result<List<MentionSuggestionDto>>.Success(none);

        var isPage = request.ResourceKind.Value == "documents.page";
        Guid boardId = Guid.Empty;
        if (!isPage)
        {
            // A deleted item has no board: nobody is told about it.
            var facts = await _items.FindAsync(workspaceId, request.ResourceId, null, ct);
            if (facts is null) return Result<List<MentionSuggestionDto>>.Success(none);
            boardId = facts.BoardId;
        }

        var query = Fold(request.Query);
        var limit = request.Limit is > 0 ? Math.Min(request.Limit.Value, GetMentionSuggestionsQuery.MaxLimit) : GetMentionSuggestionsQuery.DefaultLimit;

        // Cheap name filter first, so only likely matches cost a view check.
        var candidates = (await _actors.FindManyAsync(colleagues.Where(id => id != me).ToList(), ct))
            .Select(a => (Actor: a, Folded: Fold(a.Name)))
            .Where(c => query.Length == 0 || (" " + c.Folded).Contains(" " + query, StringComparison.Ordinal))
            .OrderBy(c => c.Folded, StringComparer.Ordinal).ThenBy(c => c.Actor.Name, StringComparer.Ordinal).ThenBy(c => c.Actor.UserId)
            .Take(MaxViewChecks)
            .Select(c => c.Actor);

        // The same per-person decision a notification delivery makes, so a suggestion is a person who would be told.
        var found = new List<MentionSuggestionDto>();
        foreach (var actor in candidates)
        {
            var canView = isPage
                ? await _pages.CanViewAsync(accountId, workspaceId, request.ResourceId, actor.UserId, ct)
                : (await _visibility.VisibleBoardIdsAsync(workspaceId, actor.UserId, ct)).Contains(boardId);
            if (!canView) continue;

            found.Add(new MentionSuggestionDto(actor.UserId, actor.Name, actor.AvatarUrl));
            if (found.Count == limit) break;
        }

        return Result<List<MentionSuggestionDto>>.Success(found);
    }

    /// <summary>Lower-case, accents removed (Đ/đ explicitly: FormD does not split them), anything but letters and digits is a word break.</summary>
    private static string Fold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Trim().Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
