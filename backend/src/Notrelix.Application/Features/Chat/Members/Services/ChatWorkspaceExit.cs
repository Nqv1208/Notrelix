using Notrelix.Application.Features.Chat.Abstractions;

namespace Notrelix.Application.Features.Chat.Members.Services;

/// <summary>
/// A person leaves a Workspace or is removed from it: every active space membership they hold in that Workspace ends with it, so
/// channels and conversations stop sending them frames and stop listing them as members. One query reads the person's memberships in
/// that one Workspace (spaces of other Workspaces are never read) and the changes are saved with the consumer's transaction.
/// A direct or group conversation left with no active participant closes, as it does on a leave (UC-CHT-E04).
/// Idempotent: a membership that already ended is not changed and gets no signal. Each space whose membership ended gets one
/// access-changed signal, so the realtime gateway closes that person's live subscription to it.
/// </summary>
public sealed class ChatWorkspaceExit(IChatDbContext db, IAccessChangeSignal access)
{
    /// <returns>The number of space memberships that ended.</returns>
    public async Task<int> EndMembershipsAsync(Guid workspaceId, Guid userId, DateTimeOffset leftAt, CancellationToken cancellationToken)
    {
        var memberships = await db.ChatMemberships
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.LeftAt == null)
            .ToListAsync(cancellationToken);

        var ended = 0;
        var touchedSpaces = new HashSet<Guid>();
        foreach (var membership in memberships)
        {
            if (!membership.EndWorkspaceExit(leftAt)) continue;

            access.ChannelPerson(workspaceId, membership.SpaceId, userId);
            touchedSpaces.Add(membership.SpaceId);
            ended++;
        }

        foreach (var spaceId in touchedSpaces)
            await CloseConversationIfEmptyAsync(workspaceId, spaceId, userId, leftAt, cancellationToken);

        return ended;
    }

    private async Task CloseConversationIfEmptyAsync(Guid workspaceId, Guid spaceId, Guid userId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var space = await db.ChatSpaces.FirstOrDefaultAsync(
            s => s.Id == spaceId && s.WorkspaceId == workspaceId && s.DeletedAt == null, cancellationToken);
        if (space is not { IsConversation: true }) return;

        // The participants load onto the tracked space, so the memberships ended above are counted as ended.
        await db.ChatMemberships.Where(m => m.SpaceId == spaceId).ToListAsync(cancellationToken);
        space.CloseIfNoParticipants(userId, at);
    }
}
