using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;

public class SaveBoardViewCommandValidator : AbstractValidator<SaveBoardViewCommand>
{
    public SaveBoardViewCommandValidator()
    {
    }
}
