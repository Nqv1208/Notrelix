namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteFormSubmission;

public class DeleteFormSubmissionCommandValidator : AbstractValidator<DeleteFormSubmissionCommand>
{
    public DeleteFormSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty();
    }
}
