using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;

public class UnarchiveBoardCommandValidator : AbstractValidator<UnarchiveBoardCommand>
{
    public UnarchiveBoardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
