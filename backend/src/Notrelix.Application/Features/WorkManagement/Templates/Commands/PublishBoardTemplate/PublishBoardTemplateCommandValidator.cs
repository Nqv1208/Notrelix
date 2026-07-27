namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.PublishBoardTemplate;

public class PublishBoardTemplateCommandValidator : AbstractValidator<PublishBoardTemplateCommand>
{
    public PublishBoardTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}
