using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Spaces;

/// <summary>A category that orders channels in the sidebar. It carries no access meaning.</summary>
public class ChannelGroup : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public double Position { get; private set; }

    private ChannelGroup() : base() { }

    public static ChannelGroup Create(Guid accountId, Guid workspaceId, string name, double position, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        ChannelGroupRules.EnsurePosition(position);

        var group = new ChannelGroup
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = ChannelGroupRules.NormalizeName(name),
            Position = position,
        };
        group.SetAuditOnCreate(createdBy, createdAt);
        return group;
    }

    public bool Update(string name, double position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);
        ChannelGroupRules.EnsurePosition(position);
        var normalized = ChannelGroupRules.NormalizeName(name);
        if (normalized == Name && position == Position) return false;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalized;
        Position = position;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }
}

internal static class ChannelGroupRules
{
    public static string NormalizeName(string? name)
    {
        Guard.NotNullOrWhiteSpace(name);
        var trimmed = name!.Trim();
        Guard.MaxLength(trimmed, ChatLimits.ChannelGroupNameMaxLength);
        return trimmed;
    }

    public static void EnsurePosition(double position)
    {
        if (!double.IsFinite(position))
            throw new BusinessRuleException(ChatRuleCodes.InvalidPosition, "Position must be a finite number.");
    }
}
