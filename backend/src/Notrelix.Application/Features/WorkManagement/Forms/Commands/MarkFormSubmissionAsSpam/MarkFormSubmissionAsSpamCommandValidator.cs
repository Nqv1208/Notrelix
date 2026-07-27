namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.MarkFormSubmissionAsSpam;

public class MarkFormSubmissionAsSpamCommandValidator : AbstractValidator<MarkFormSubmissionAsSpamCommand>
{
    public MarkFormSubmissionAsSpamCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty();
    }
}
