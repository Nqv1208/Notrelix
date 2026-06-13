using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Collaboration.Reactions;

public class Reaction : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public Emoji Emoji { get; private set; } = null!;

    private Reaction() : base() { }

    public static Reaction Create(Guid workspaceId, ResourceRef target, Guid userId, Emoji emoji, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotEmpty(userId);
        Guard.NotNull(emoji);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        var reaction = new Reaction
        {
            WorkspaceId = workspaceId,
            Target = target,
            UserId = userId,
            Emoji = emoji
        };

        reaction.SetAuditOnCreate(userId, createdAt);
        reaction.AddDomainEvent(new ReactionCreatedEvent(workspaceId, reaction.Id, target, userId, emoji, createdAt));
        return reaction;
    }

    public void Remove(DateTimeOffset removedAt)
    {
        AddDomainEvent(new ReactionRemovedEvent(WorkspaceId, Id, Target, UserId, Emoji, removedAt));
    }
}
