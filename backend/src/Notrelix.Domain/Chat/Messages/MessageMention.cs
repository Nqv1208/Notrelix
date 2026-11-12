namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// One stored mention of a message (CHT-010, UC-CHT-G01, G03, G04): the kind, where the text sits and, for a person or a channel,
/// the stable id. The rows of a message are replaced as a set when the author edits it and removed when it is deleted. A row grants
/// nothing and notifies nobody by itself: a person who cannot see the space is stored here (COL-030) and simply never counted.
/// </summary>
public sealed class MessageMention : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid MessageId { get; private set; }
    public MentionKind Kind { get; private set; }

    /// <summary>The person or the channel. Null for a broadcast.</summary>
    public Guid? TargetId { get; private set; }

    public int Start { get; private set; }
    public int Length { get; private set; }

    private MessageMention() : base() { }

    /// <summary>A mention from a span <see cref="MentionSpans.Normalize"/> accepted and the handler resolved.</summary>
    public static MessageMention Create(Guid accountId, Guid workspaceId, Guid spaceId, Guid messageId, MentionSpan span)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(messageId);
        Guard.NotNull(span);

        return new MessageMention
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            MessageId = messageId,
            Kind = span.Kind,
            TargetId = span.TargetId,
            Start = span.Start,
            Length = span.Length,
        };
    }

    public MentionSpan ToSpan() => new(Kind, Start, Length, TargetId);
}
