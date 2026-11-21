using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Permissions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Calls;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Calls.Services;

/// <summary>
/// When a channel's permission overrides change, everyone in its live call is checked again against the new rules (the same post right
/// that starts and joins a call), and each person's right to publish reaches the media room as an outbox fact. A channel turned
/// read-only therefore mutes the people already talking at once instead of when their token runs out. The space already holds the
/// new overrides in memory, so the answer does not wait for the save.
/// </summary>
public sealed class ChatCallPermissionRefresh(IChatDbContext db, IWorkspaceMemberFacts members)
{
    public async Task RefreshAsync(Space space, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var call = await db.ChatCalls.Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.SpaceId == space.Id && c.State != CallState.Ended, cancellationToken);
        if (call is null) return;

        foreach (var person in call.Participants.Where(p => p.IsJoined).Select(p => p.PersonId).ToList())
        {
            var fact = await members.FindAsync(space.WorkspaceId, person, cancellationToken);
            if (fact is not { IsActive: true }) continue; // access ended: the revocation path drops them

            var membership = space.Memberships.FirstOrDefault(m => m.UserId == person && m.IsActive);
            var canPublish = ChannelPermissionChecks.Capabilities(space, fact.Role, membership).CanPost;
            call.ChangePublishPermission(person, canPublish, at);
        }
    }
}
