namespace Notrelix.Application.Features.Chat.Public.SpaceViewFacts;

/// <summary>
/// Chat-published fact: which chat spaces a person can see now, and what they are called. The Inbox uses it to show a chat notification
/// only while its recipient is still an active member of the space (a historical notification never keeps access alive), and to name the
/// channel. Chat decides visibility; the consumer never reads Chat persistence. A direct or group conversation has no name (null). A space
/// the person cannot see, or that no longer exists, is absent.
/// </summary>
public interface IChatSpaceViewFacts
{
    Task<IReadOnlyDictionary<Guid, string?>> ViewableSpacesAsync(
        Guid workspaceId, Guid userId, IReadOnlyCollection<Guid> spaceIds, CancellationToken cancellationToken);
}
