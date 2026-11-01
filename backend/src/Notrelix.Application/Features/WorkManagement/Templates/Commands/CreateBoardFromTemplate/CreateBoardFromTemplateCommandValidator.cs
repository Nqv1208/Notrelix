namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;

public class CreateBoardFromTemplateCommandValidator : AbstractValidator<CreateBoardFromTemplateCommand>
{
    public CreateBoardFromTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(global::Notrelix.Domain.WorkManagement.Boards.Board.MaxTitleLength);
        RuleFor(x => x.SpaceId).NotEqual(Guid.Empty).When(x => x.SpaceId.HasValue);
    }
}
