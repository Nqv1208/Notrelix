using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

public class UpdateBoardCommandValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardCommandValidator()
    {
    }
}
