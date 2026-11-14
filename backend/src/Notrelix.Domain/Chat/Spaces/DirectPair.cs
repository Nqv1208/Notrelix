using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Spaces;

/// <summary>
/// The unordered pair of two people who share a direct conversation in a Workspace (CHT-014, UC-CHT-E01). The pair is stored
/// with the lower id first, so (Workspace, low, high) is unique and names one direct conversation. Owned by no aggregate: the
/// start command writes it under a pair lock and the unique key is the last line of defence.
/// </summary>
public sealed class DirectPair : IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserLow { get; private set; }
    public Guid UserHigh { get; private set; }
    public Guid SpaceId { get; private set; }

    private DirectPair() { }

    /// <summary>Orders two distinct person ids as (low, high). Rejects a pair of the same person (400).</summary>
    public static (Guid Low, Guid High) Order(Guid first, Guid second)
    {
        Guard.NotEmpty(first);
        Guard.NotEmpty(second);
        if (first == second)
            throw new BusinessRuleException(ChatRuleCodes.SelfTarget, "A direct conversation needs two different people.");

        return first.CompareTo(second) < 0 ? (first, second) : (second, first);
    }

    public static DirectPair Create(Guid accountId, Guid workspaceId, Guid first, Guid second, Guid spaceId)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        var (low, high) = Order(first, second);

        return new DirectPair
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserLow = low,
            UserHigh = high,
            SpaceId = spaceId,
        };
    }
}
