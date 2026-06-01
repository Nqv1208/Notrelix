using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.DuplicateList;

public class DuplicateListCommandValidator : AbstractValidator<DuplicateListCommand>
{
    public DuplicateListCommandValidator()
    {
    }
}
