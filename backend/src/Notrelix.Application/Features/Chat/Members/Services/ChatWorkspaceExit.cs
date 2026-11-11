using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Chat.Abstractions;

namespace Notrelix.Application.Features.Chat.Members.Services;

/// <summary>
/// A person leaves a Workspace or is removed from it: every active channel membership they hold in that Workspace ends with it,
/// so the channels stop sending them frames and stop listing them as members. One query reads the person's memberships in that
/// one Workspace (channels of other Workspaces are never read) and the changes are saved with the consumer's transaction.
/// Idempotent: a membership that already ended is not changed and gets no signal. Each channel whose membership ended gets one
/// access-changed signal, so the realtime gateway closes that person's live subscription to it.
/// </summary>
public sealed class ChatWorkspaceExit(IChatDbContext db, IAccessChangeSignal access)
{
    /// <returns>The number of channel memberships that ended.</returns>
    public async Task<int> EndMembershipsAsync(Guid workspaceId, Guid userId, DateTimeOffset leftAt, CancellationToken cancellationToken)
    {
        var memberships = await db.ChatMemberships
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.LeftAt == null)
            .ToListAsync(cancellationToken);

        var ended = 0;
        foreach (var membership in memberships)
        {
            if (!membership.EndWorkspaceExit(leftAt)) continue;

            access.ChannelPerson(workspaceId, membership.SpaceId, userId);
            ended++;
        }
        return ended;
    }
}
