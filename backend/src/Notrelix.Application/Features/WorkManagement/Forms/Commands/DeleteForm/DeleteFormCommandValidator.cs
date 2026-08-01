namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;

public class DeleteFormCommandValidator : AbstractValidator<DeleteFormCommand>
{
    public DeleteFormCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();
    }
}
