namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;

public class CreateBoardFromTemplateCommandValidator : AbstractValidator<CreateBoardFromTemplateCommand>
{
    public CreateBoardFromTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
