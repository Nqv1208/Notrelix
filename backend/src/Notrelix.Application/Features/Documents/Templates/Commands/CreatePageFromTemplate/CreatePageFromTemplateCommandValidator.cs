namespace Notrelix.Application.Features.Documents.Templates.Commands.CreatePageFromTemplate;

public class CreatePageFromTemplateCommandValidator : AbstractValidator<CreatePageFromTemplateCommand>
{
    public CreatePageFromTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500);
    }
}
