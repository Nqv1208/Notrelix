namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.DeleteApprovalRequest;

public class DeleteApprovalRequestCommandValidator : AbstractValidator<DeleteApprovalRequestCommand>
{
    public DeleteApprovalRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();
    }
}
