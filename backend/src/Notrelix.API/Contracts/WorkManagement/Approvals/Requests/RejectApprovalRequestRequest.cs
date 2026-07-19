namespace Notrelix.API.Contracts.WorkManagement.Approvals.Requests;

public sealed record RejectApprovalRequestRequest(string? Note, long ExpectedVersion);
