using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Spaces;

/// <summary>
/// One person's block of another in a Workspace (UC-CHT-E06). The blocker keeps the block; it stops a direct conversation
/// between the two from being started by either side (the blocked person sees NotFound). It does not hide any channel message.
/// Keyed by (Workspace, blocker, blocked): one row per direction, so blocking twice changes nothing.
/// </summary>
public sealed class PersonBlock : IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BlockerId { get; private set; }
    public Guid BlockedId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PersonBlock() { }

    public static PersonBlock Create(Guid accountId, Guid workspaceId, Guid blockerId, Guid blockedId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(blockerId);
        Guard.NotEmpty(blockedId);
        if (blockerId == blockedId)
            throw new BusinessRuleException(ChatRuleCodes.SelfTarget, "A person cannot block themselves.");

        return new PersonBlock
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAt = createdAt,
        };
    }
}
