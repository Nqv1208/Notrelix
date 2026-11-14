using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// One person's reaction to a message with one kind (CHT-011, UC-CHT-G06, G07). The identity of a reaction is the triple
/// message, person and kind: a person has at most one reaction of a kind on a message (unique on the triple), so a retry or a
/// double click is one reaction. The row is removed by the caller; <see cref="Remove"/> only raises the fact.
/// </summary>
public sealed class Reaction : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Reaction() : base() { }

    /// <summary>
    /// A reaction the caller has checked is allowed (see <see cref="Message.EnsureReactable"/> and <see cref="EnsureRoom"/>).
    /// The kind must be on the palette.
    /// </summary>
    public static Reaction Create(Guid accountId, Guid workspaceId, Guid spaceId, Guid messageId, Guid userId, string kind, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(messageId);
        Guard.NotEmpty(userId);
        EnsureKind(kind);

        var reaction = new Reaction
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            MessageId = messageId,
            UserId = userId,
            Kind = kind,
            CreatedAt = createdAt,
        };
        reaction.RaiseDomainEvent(new MessageReactionChangedDomainEvent(accountId, workspaceId, spaceId, messageId, userId, kind, Added: true, createdAt));
        return reaction;
    }

    /// <summary>Records the end of the reaction. The caller removes the row; this method only raises the fact.</summary>
    public void Remove(Guid removedBy, DateTimeOffset removedAt)
    {
        Guard.NotEmpty(removedBy);
        RaiseDomainEvent(new MessageReactionChangedDomainEvent(AccountId, WorkspaceId, SpaceId, MessageId, removedBy, Kind, Added: false, removedAt));
    }

    /// <summary>
    /// Refuses a kind that is not on the palette. The handler checks this before any read, so an unknown kind is a 400 whatever the
    /// message is.
    /// </summary>
    public static void EnsureKind(string? kind)
    {
        if (!ReactionKinds.Contains(kind))
            throw new BusinessRuleException(ChatRuleCodes.ReactionKindInvalid, "The emoji is not in the reaction palette.");
    }

    /// <summary>
    /// Refuses a new reaction once the message holds the limits. A kind the message already has from someone else does not count
    /// against the distinct limit; a kind the caller already has is a no-op before this check runs.
    /// </summary>
    public static void EnsureRoom(int distinctKindsOnMessage, int reactionsByActorOnMessage, bool kindIsNew)
    {
        if (kindIsNew && distinctKindsOnMessage >= ChatLimits.ReactionKindsPerMessageMax)
            throw new BusinessRuleException(ChatRuleCodes.LimitReached, $"A message holds at most {ChatLimits.ReactionKindsPerMessageMax} distinct reactions.");
        if (reactionsByActorOnMessage >= ChatLimits.ReactionsPerUserPerMessageMax)
            throw new BusinessRuleException(ChatRuleCodes.LimitReached, $"A person holds at most {ChatLimits.ReactionsPerUserPerMessageMax} reactions on one message.");
    }
}
