namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.SoftDeleteApprovalRequest;

public class SoftDeleteApprovalRequestCommandValidator : AbstractValidator<SoftDeleteApprovalRequestCommand>
{
    public SoftDeleteApprovalRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();
    }
}
