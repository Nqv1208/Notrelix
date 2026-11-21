using Notrelix.Application.Features.Collaboration.Public.WorkspaceActivity;

namespace Notrelix.Application.Features.Collaboration.Activity;

/// <summary>
/// Decides which resource a recorded activity row is about, so the workspace feed can show it only to a reader of that
/// resource. A row whose Board or Page cannot be named is <see cref="WorkspaceActivityScopeKind.Unresolved"/> and is never
/// shown (the stricter answer): an Item comment or mention carries the Item, not its Board.
/// </summary>
public static class WorkspaceActivityScopes
{
    private const string BoardItemKind = "work-management.board-item";
    private const string PageKind = "documents.page";
    private const string BoardSubject = "Board";
    private const string WorkspaceContext = "workspace";

    public static WorkspaceActivityScope Of(
        string sourceContext, string subjectType, Guid subjectId, string? targetType, Guid? targetId,
        string? resourceKind, Guid? resourceId)
    {
        if (resourceKind == PageKind && resourceId is { } page) return new(WorkspaceActivityScopeKind.Page, page);
        if (resourceKind == BoardItemKind && targetType == BoardSubject && targetId is { } itemBoard)
            return new(WorkspaceActivityScopeKind.Board, itemBoard);
        if (subjectType == BoardSubject) return new(WorkspaceActivityScopeKind.Board, subjectId);
        if (targetType == PageKind && targetId is { } commentedPage) return new(WorkspaceActivityScopeKind.Page, commentedPage);
        if (sourceContext == WorkspaceContext) return new(WorkspaceActivityScopeKind.Workspace, null);
        return new(WorkspaceActivityScopeKind.Unresolved, null);
    }
}
