namespace Notrelix.API.Contracts.WorkManagement.Approvals.Requests;

public record ApprovalStepRequest(Guid? ApproverUserId, Guid? ApproverTeamId);

public record CreateApprovalRequestRequest(string Title, string? Description, List<ApprovalStepRequest>? Steps);
