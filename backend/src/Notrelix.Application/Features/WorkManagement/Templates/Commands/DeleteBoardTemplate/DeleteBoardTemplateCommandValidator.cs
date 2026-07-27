namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.DeleteBoardTemplate;

public class DeleteBoardTemplateCommandValidator : AbstractValidator<DeleteBoardTemplateCommand>
{
    public DeleteBoardTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}
