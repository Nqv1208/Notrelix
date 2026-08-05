namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;

public class RestoreFormCommandValidator : AbstractValidator<RestoreFormCommand>
{
    public RestoreFormCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();
    }
}
