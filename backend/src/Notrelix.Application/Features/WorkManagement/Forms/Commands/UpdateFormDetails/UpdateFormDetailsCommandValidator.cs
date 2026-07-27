namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;

public class UpdateFormDetailsCommandValidator : AbstractValidator<UpdateFormDetailsCommand>
{
    public UpdateFormDetailsCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
    }
}
