namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;

public class CloseFormCommandValidator : AbstractValidator<CloseFormCommand>
{
    public CloseFormCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();
    }
}
