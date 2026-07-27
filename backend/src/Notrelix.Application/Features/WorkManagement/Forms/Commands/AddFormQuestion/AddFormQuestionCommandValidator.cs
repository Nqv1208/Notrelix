namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;

public class AddFormQuestionCommandValidator : AbstractValidator<AddFormQuestionCommand>
{
    public AddFormQuestionCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();

        RuleFor(x => x.QuestionKey)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(500);
    }
}
