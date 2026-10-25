namespace Notrelix.API.Contracts.WorkManagement.Approvals.Requests;

/// <summary>The decision on an approval request: <c>approve</c> or <c>reject</c>, with an optional comment.</summary>
public sealed record DecideApprovalRequest(string Decision, string? Comment);
