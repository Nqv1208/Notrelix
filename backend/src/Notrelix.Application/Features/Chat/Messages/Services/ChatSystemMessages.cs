using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Domain.Chat.Messages;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Messages.Services;

/// <summary>
/// Stores the system message that announces a change in a channel (UC-CHT-B09). The channel commands call it inside
/// their own transaction, so the announcement and the change are stored together or not at all. The message is
/// attributed to the person who made the change.
/// </summary>
public sealed class ChatSystemMessages(
    IChatDbContext db,
    ISpaceSequenceAllocator sequences,
    IDateTimeProvider clock)
{
    public const string MemberJoined = "joined the channel";
    public const string MemberLeft = "left the channel";
    public const string MemberRemoved = "was removed from the channel";
    public const string MemberAdded = "added a member to the channel";
    public const string Archived = "archived the channel";
    public const string Unarchived = "unarchived the channel";
    public const string MessagePinned = "pinned a message";

    public static string Renamed(string name) => $"renamed the channel to {name}";

    public async Task AnnounceAsync(Space space, Guid actorId, string content, CancellationToken cancellationToken)
    {
        var sequence = await sequences.NextAsync(space.Id, cancellationToken);
        var message = Message.CreateSystemMessage(
            space.AccountId,
            space.WorkspaceId,
            space.Id,
            sequence,
            actorId,
            content,
            clock.UtcNow);

        db.ChatMessages.Add(message);
    }
}
