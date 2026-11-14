using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Conversations.Services;

/// <summary>
/// Who may see and start a conversation. A conversation is visible to its participants only (CHT-004): Owner and Admin do not see
/// it by role. Every refusal is the same NotFound as a missing conversation, so a caller learns nothing about another pair.
/// </summary>
public static class ConversationAccess
{
    /// <summary>A live conversation of the Workspace the caller participates in; anything else is NotFound.</summary>
    public static async Task<Space> LoadParticipantAsync(IChatDbContext db, Guid workspaceId, Guid conversationId, Guid userId, CancellationToken ct)
    {
        var space = await ChannelAccess.LoadLiveAsync(db, workspaceId, conversationId, ct);
        if (!space.IsConversation || !space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), conversationId);
        return space;
    }

    /// <summary>
    /// A Guest reaches a person only through a space they were both added to (CHT-004). A person who is not a Guest may start a
    /// conversation with any active member of the Workspace.
    /// </summary>
    public static bool MayReach(WorkspaceMemberFact caller, bool sharesSpace) =>
        !string.Equals(caller.Role, ChannelAccess.GuestRole, StringComparison.Ordinal) || sharesSpace;

    /// <summary>True when both people hold an active membership in one live space of the Workspace.</summary>
    public static Task<bool> SharesSpaceAsync(IChatDbContext db, Guid workspaceId, Guid first, Guid second, CancellationToken ct) =>
        db.ChatMemberships.AsNoTracking().AnyAsync(m =>
            m.WorkspaceId == workspaceId && m.UserId == first && m.LeftAt == null
            && db.ChatMemberships.Any(o => o.SpaceId == m.SpaceId && o.UserId == second && o.LeftAt == null)
            && db.ChatSpaces.Any(s => s.Id == m.SpaceId && s.DeletedAt == null), ct);

    /// <summary>True when either person has blocked the other in the Workspace (UC-CHT-E06): a block stops a new direct conversation both ways.</summary>
    public static Task<bool> BlockedBetweenAsync(IChatDbContext db, Guid workspaceId, Guid first, Guid second, CancellationToken ct) =>
        db.ChatBlocks.AsNoTracking().AnyAsync(b =>
            b.WorkspaceId == workspaceId
            && ((b.BlockerId == first && b.BlockedId == second) || (b.BlockerId == second && b.BlockedId == first)), ct);
}

/// <summary>A message in a conversation shows it again for everyone who hid it (UC-CHT-E07, "a new message restores it").</summary>
public static class ConversationVisibility
{
    public static void ShowAgain(Space space)
    {
        if (!space.IsConversation) return;

        foreach (var membership in space.Memberships.Where(m => m.IsActive && m.IsHidden))
            membership.Restore();
    }
}
