using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.UnarchiveList;

public class UnarchiveListCommandValidator : AbstractValidator<UnarchiveListCommand>
{
    public UnarchiveListCommandValidator()
    {
    }
}
