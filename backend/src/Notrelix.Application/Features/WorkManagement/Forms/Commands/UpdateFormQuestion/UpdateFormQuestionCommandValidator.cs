namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;

public class UpdateFormQuestionCommandValidator : AbstractValidator<UpdateFormQuestionCommand>
{
    public UpdateFormQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty();

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(500);
    }
}
