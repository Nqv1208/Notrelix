namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;

public class ProcessFormSubmissionCommandValidator : AbstractValidator<ProcessFormSubmissionCommand>
{
    public ProcessFormSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty();

        RuleFor(x => x.CreatedItemId)
            .NotEmpty();
    }
}
