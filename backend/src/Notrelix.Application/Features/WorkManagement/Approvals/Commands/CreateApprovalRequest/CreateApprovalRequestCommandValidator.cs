namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.CreateApprovalRequest;

public class CreateApprovalRequestCommandValidator : AbstractValidator<CreateApprovalRequestCommand>
{
    public CreateApprovalRequestCommandValidator()
    {
        RuleFor(x => x.TargetResourceId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
    }
}
