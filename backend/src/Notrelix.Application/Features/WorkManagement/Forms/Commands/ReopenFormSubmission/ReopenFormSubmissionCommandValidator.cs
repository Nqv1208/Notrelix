namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.ReopenFormSubmission;

public class ReopenFormSubmissionCommandValidator : AbstractValidator<ReopenFormSubmissionCommand>
{
    public ReopenFormSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty();
    }
}
