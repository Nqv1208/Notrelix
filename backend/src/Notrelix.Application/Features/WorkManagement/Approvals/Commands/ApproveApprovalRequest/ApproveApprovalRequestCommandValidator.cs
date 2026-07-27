namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;

public class ApproveApprovalRequestCommandValidator : AbstractValidator<ApproveApprovalRequestCommand>
{
    public ApproveApprovalRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();
    }
}
