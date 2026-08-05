namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.ArchiveBoardTemplate;

public class ArchiveBoardTemplateCommandValidator : AbstractValidator<ArchiveBoardTemplateCommand>
{
    public ArchiveBoardTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}
