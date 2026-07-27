namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;

public class RejectApprovalRequestCommandValidator : AbstractValidator<RejectApprovalRequestCommand>
{
    public RejectApprovalRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();
    }
}
