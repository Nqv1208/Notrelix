using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;

namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>
/// The plan's automation-rule slot is held only by a rule that is enabled (or that the platform
/// auto-disabled and that therefore still holds its slot). Draft and manually Disabled rules use no
/// slot; enabling consumes one, disabling or deleting an enabled rule releases it.
/// </summary>
public static class AutomationRuleCapacity
{
    /// <summary>
    /// True while the rule holds its slot. A platform auto-disable keeps the slot (the rule carries a
    /// <see cref="AutomationRule.DisabledReason"/>) until a person enables, disables or deletes it.
    /// </summary>
    public static bool HoldsSlot(AutomationRule rule) => rule.IsEnabled || rule.DisabledReason is not null;

    /// <summary>
    /// The operation identity of one capacity effect. It names the rule, the step and the rule
    /// version the step started from, so a retry of the same step replays once while the next
    /// enable, disable or delete of the same rule is a new effect.
    /// </summary>
    public static BillingCapacityOperationIdentity Identity(
        AutomationRule rule,
        string step,
        long fromVersion,
        Guid actorUserId,
        DateTimeOffset occurredAt) =>
        new(
            AccountId: rule.AccountId,
            WorkspaceId: rule.WorkspaceId,
            CapabilityCode: BillingCapabilityCode.AutomationRule,
            Amount: 1,
            LogicalOperationId: OperationId(rule.Id, step, fromVersion),
            SourceResource: rule.Id.ToString(),
            ActorUserId: actorUserId,
            OccurredAt: occurredAt);

    private static Guid OperationId(Guid ruleId, string step, long fromVersion) =>
        new(System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes($"automation-rule-{step}:{ruleId:N}:{fromVersion}")));
}
