namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;

public class RejectFormSubmissionCommandValidator : AbstractValidator<RejectFormSubmissionCommand>
{
    public RejectFormSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty();
    }
}
