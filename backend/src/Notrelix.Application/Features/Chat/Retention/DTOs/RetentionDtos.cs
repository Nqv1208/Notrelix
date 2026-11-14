using Notrelix.Domain.Chat.Retention;

namespace Notrelix.Application.Features.Chat.Retention.DTOs;

/// <summary>
/// The Workspace's chat retention rule and its version, which the next set must send as expectedVersion (UC-CHT-H05). A Workspace that never
/// set a rule reads as no retention at version 1. CanChange tells the viewer whether they may set it (the Workspace Owner), so a client
/// shows a read-only view to everyone else; the server still decides.
/// </summary>
public sealed record RetentionPolicyDto(int? RetentionDays, string Mode, long Version, DateTimeOffset? UpdatedAt, bool CanChange)
{
    public static RetentionPolicyDto From(RetentionPolicy policy, bool canChange) =>
        new(policy.RetentionDays, policy.Mode.ToWire(), policy.Version, policy.UpdatedAt, canChange);

    public static RetentionPolicyDto None(bool canChange) =>
        new(null, RetentionMode.Tombstone.ToWire(), 1, null, canChange);
}
