namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.SoftDeleteForm;

public class SoftDeleteFormCommandValidator : AbstractValidator<SoftDeleteFormCommand>
{
    public SoftDeleteFormCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();
    }
}
