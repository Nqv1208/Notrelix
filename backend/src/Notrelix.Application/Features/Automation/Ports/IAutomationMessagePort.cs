namespace Notrelix.Application.Features.Automation.Ports;

/// <summary>
/// Automation-owned port for the two actions that tell a person something: an in-app
/// notification and an email. Both are idempotent by <c>operationId</c>, so a redelivered
/// execution step never tells the person twice.
/// </summary>
public interface IAutomationMessagePort
{
    /// <summary>Raises an in-app notification for the member (their notification preferences still apply).</summary>
    Task NotifyMemberAsync(
        Guid accountId, Guid workspaceId, Guid recipientUserId, Guid? itemId, Guid? actorUserId,
        Guid operationId, string title, string? body, CancellationToken cancellationToken);

    /// <summary>
    /// Queues an email to the member. Returns false when the member has no address on record
    /// (nothing is queued).
    /// </summary>
    Task<bool> SendEmailAsync(
        Guid accountId, Guid workspaceId, Guid recipientUserId, Guid operationId,
        string subject, string? body, CancellationToken cancellationToken);
}
