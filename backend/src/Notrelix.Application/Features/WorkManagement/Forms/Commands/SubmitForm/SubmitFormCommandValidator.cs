namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;

public class SubmitFormCommandValidator : AbstractValidator<SubmitFormCommand>
{
    public SubmitFormCommandValidator()
    {
        RuleFor(x => x.FormId).NotEmpty();
        RuleFor(x => x.Answers).NotNull().Must(a => a is null || a.Count <= 200).WithMessage("Too many answers.");
    }
}
