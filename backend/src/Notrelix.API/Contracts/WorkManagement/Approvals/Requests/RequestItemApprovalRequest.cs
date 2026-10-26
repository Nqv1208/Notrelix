namespace Notrelix.API.Contracts.WorkManagement.Approvals.Requests;

/// <summary>Ask one person to approve an Item, with an optional note.</summary>
public sealed record RequestItemApprovalRequest(Guid ApproverUserId, string? Note);
