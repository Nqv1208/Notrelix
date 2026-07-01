namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;

public class SaveBoardViewCommandValidator : AbstractValidator<SaveBoardViewCommand>
{
    public SaveBoardViewCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty();

        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.ViewMode)
            .IsInEnum();
    }
}
