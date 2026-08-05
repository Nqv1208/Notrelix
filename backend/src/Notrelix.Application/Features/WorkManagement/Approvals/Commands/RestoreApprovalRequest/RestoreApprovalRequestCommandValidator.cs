namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.RestoreApprovalRequest;

public class RestoreApprovalRequestCommandValidator : AbstractValidator<RestoreApprovalRequestCommand>
{
    public RestoreApprovalRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();
    }
}
