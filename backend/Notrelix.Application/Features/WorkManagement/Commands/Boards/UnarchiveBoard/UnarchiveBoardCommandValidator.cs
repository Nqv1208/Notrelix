using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.UnarchiveBoard;

public class UnarchiveBoardCommandValidator : AbstractValidator<UnarchiveBoardCommand>
{
    public UnarchiveBoardCommandValidator()
    {
    }
}
