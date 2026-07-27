namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;

public class CancelApprovalRequestCommandValidator : AbstractValidator<CancelApprovalRequestCommand>
{
    public CancelApprovalRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();
    }
}
