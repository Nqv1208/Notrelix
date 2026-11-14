using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.RateLimits;

/// <summary>
/// A channel's slow mode (UC-CHT-H08): the least time between two messages of one person. A send and a thread reply count as
/// messages; the time is read from the person's own last message in the space, so a deleted message does not reset it. Nobody is
/// exempt (the use case names no exemption): a Workspace Owner, an Admin and a channel manager wait like anyone.
/// </summary>
public static class SlowMode
{
    public static async Task RequireAsync(
        IChatDbContext db, Space space, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (space.SlowModeSeconds <= 0) return;

        var last = await db.ChatMessages.AsNoTracking()
            .Where(m => m.SpaceId == space.Id && m.AuthorId == userId && m.Kind == MessageKind.User)
            .MaxAsync(m => (DateTimeOffset?)m.CreatedAt, cancellationToken);
        if (last is not { } at) return;

        var wait = at.AddSeconds(space.SlowModeSeconds) - now;
        if (wait <= TimeSpan.Zero) return;

        throw new TooManyRequestsException(
            $"This channel is in slow mode. Wait {Math.Ceiling(wait.TotalSeconds):0} seconds to write again.", ChatRuleCodes.SlowMode, wait)
        {
            Extensions = new Dictionary<string, object?> { ["slowModeSeconds"] = space.SlowModeSeconds },
        };
    }
}
