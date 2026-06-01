using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.CreateList;

public class CreateListCommandValidator : AbstractValidator<CreateListCommand>
{
    public CreateListCommandValidator()
    {
    }
}
