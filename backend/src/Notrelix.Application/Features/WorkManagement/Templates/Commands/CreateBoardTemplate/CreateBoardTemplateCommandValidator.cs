namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;

public class CreateBoardTemplateCommandValidator : AbstractValidator<CreateBoardTemplateCommand>
{
    public CreateBoardTemplateCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
