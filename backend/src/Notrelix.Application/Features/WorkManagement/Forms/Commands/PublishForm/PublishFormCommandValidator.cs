namespace Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;

public class PublishFormCommandValidator : AbstractValidator<PublishFormCommand>
{
    public PublishFormCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();
    }
}
