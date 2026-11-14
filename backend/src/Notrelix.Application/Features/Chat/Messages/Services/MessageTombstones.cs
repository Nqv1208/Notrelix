using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Domain.Chat.Messages;

namespace Notrelix.Application.Features.Chat.Messages.Services;

/// <summary>
/// What goes with a message when it becomes a tombstone, whoever deleted it (UC-CHT-B05, H03): its pin, reactions, mentions and files.
/// The author's delete and the moderator's delete call it, so the two leave the same tombstone.
/// </summary>
public static class MessageTombstones
{
    public static async Task ClearAttachedAsync(
        IChatDbContext db, IChatMessageAttachments attachments, Message message, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        var pins = await db.ChatPins.Where(p => p.MessageId == message.Id).ToListAsync(ct);
        foreach (var pin in pins)
        {
            pin.Remove(actorId, now);
            db.ChatPins.Remove(pin);
        }

        // A tombstone carries no reactions. The message's delete fact already tells readers; the reaction rows go without
        // a fact of their own, so one deletion is one announcement.
        var reactions = await db.ChatReactions.Where(r => r.MessageId == message.Id).ToListAsync(ct);
        foreach (var reaction in reactions)
            db.ChatReactions.Remove(reaction);

        // A tombstone carries no mentions either, so a deleted message never counts as a mention.
        var mentions = await db.ChatMessageMentions.Where(m => m.MessageId == message.Id).ToListAsync(ct);
        foreach (var mention in mentions)
            db.ChatMessageMentions.Remove(mention);

        // A tombstone carries no files either (UC-CHT-G08): the attachments of the message are removed with it, and the retention
        // job purges the stored objects. Nobody can open them from now on, because the shared flow no longer finds them.
        await attachments.RemoveAllAsync(message.Id, ct);
    }
}
