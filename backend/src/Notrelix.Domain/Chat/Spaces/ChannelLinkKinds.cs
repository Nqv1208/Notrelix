namespace Notrelix.Domain.Chat.Spaces;

/// <summary>
/// The ResourceRef kinds a channel can be linked to (UC-CHT-H10, CHT-023). The codes are persisted in <c>chat.spaces.link_kind</c>
/// and are part of the contract: never rename or reuse one.
/// </summary>
public static class ChannelLinkKinds
{
    public const string Board = "work-management.board";
    public const string Page = "documents.page";
    public const string Team = "governance.team";

    public static readonly IReadOnlyList<string> All = [Board, Page, Team];

    public static bool IsKnown(string? kind) => kind is not null && All.Contains(kind, StringComparer.Ordinal);
}
