namespace Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;

public class CreatePageTemplateCommandValidator : AbstractValidator<CreatePageTemplateCommand>
{
    public CreatePageTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(1024);
    }
}
