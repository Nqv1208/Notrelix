using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.UpdateList;

public class UpdateListCommandValidator : AbstractValidator<UpdateListCommand>
{
    public UpdateListCommandValidator()
    {
    }
}
