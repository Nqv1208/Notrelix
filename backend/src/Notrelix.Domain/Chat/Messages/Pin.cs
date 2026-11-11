using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// A message pinned in its space (CHT-006, UC-CHT-F06, F07). A message has at most one pin (unique on the message), and a
/// space holds at most <see cref="ChatLimits.PinLimit"/> pins; the handler checks the limit under the space lock. A pin ends
/// when the message is unpinned or deleted, and both changes raise the same fact so realtime can tell readers.
/// </summary>
public sealed class Pin : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid PinnedBy { get; private set; }
    public DateTimeOffset PinnedAt { get; private set; }

    private Pin() : base() { }

    /// <summary>A pin of a message the caller has checked is pinnable (see <see cref="Message.EnsurePinnable"/>).</summary>
    public static Pin Create(Guid accountId, Guid workspaceId, Guid spaceId, Guid messageId, Guid pinnedBy, DateTimeOffset pinnedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(messageId);
        Guard.NotEmpty(pinnedBy);

        var pin = new Pin
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            MessageId = messageId,
            PinnedBy = pinnedBy,
            PinnedAt = pinnedAt,
        };
        pin.RaiseDomainEvent(new MessagePinChangedDomainEvent(accountId, workspaceId, spaceId, messageId, pinnedBy, Pinned: true, pinnedAt));
        return pin;
    }

    /// <summary>
    /// Records the end of the pin, by an unpin or by the deletion of the message. The caller removes the row; this method
    /// only raises the fact.
    /// </summary>
    public void Remove(Guid removedBy, DateTimeOffset removedAt)
    {
        Guard.NotEmpty(removedBy);
        RaiseDomainEvent(new MessagePinChangedDomainEvent(AccountId, WorkspaceId, SpaceId, MessageId, removedBy, Pinned: false, removedAt));
    }

    /// <summary>Refuses a pin once the space holds the limit. The count is taken by the caller under the space lock.</summary>
    public static void EnsureRoom(int pinsInSpace)
    {
        if (pinsInSpace >= ChatLimits.PinLimit)
            throw new BusinessRuleException(ChatRuleCodes.LimitReached, $"A channel holds at most {ChatLimits.PinLimit} pins.");
    }
}
