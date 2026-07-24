using Notrelix.Domain.Collaboration.Reactions.Events;
namespace Notrelix.Domain.Collaboration.Reactions;

public class Reaction : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public Emoji Emoji { get; private set; } = null!;

    private Reaction() : base() { }

    public static Reaction Create(Guid accountId, Guid workspaceId, ResourceRef target, Guid userId, Emoji emoji, DateTimeOffset createdAt, Func<Guid, bool>? checkDuplicate = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotEmpty(userId);
        Guard.NotNull(emoji);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(BusinessRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        if (checkDuplicate != null && checkDuplicate(userId))
            throw new BusinessRuleException(BusinessRuleCodes.Collaboration_Reaction_DuplicateReaction, "User has already reacted with this emoji to this target.");

        var reaction = new Reaction
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            UserId = userId,
            Emoji = emoji
        };

        reaction.SetAuditOnCreate(userId, createdAt);
        reaction.RaiseDomainEvent(new ReactionCreatedDomainEvent(accountId, workspaceId, reaction.Id, target, userId, emoji, createdAt));
        return reaction;
    }

    public void Remove(DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        RaiseDomainEvent(new ReactionRemovedDomainEvent(AccountId, WorkspaceId, Id, Target, UserId, Emoji, removedAt));
    }
}
