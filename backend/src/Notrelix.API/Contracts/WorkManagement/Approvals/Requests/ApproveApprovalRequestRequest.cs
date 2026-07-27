namespace Notrelix.API.Contracts.WorkManagement.Approvals.Requests;

public sealed record ApproveApprovalRequestRequest(string? Note, long ExpectedVersion);
