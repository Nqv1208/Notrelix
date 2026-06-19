using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

public class DuplicateCardCommandValidator : AbstractValidator<DuplicateBoardItemCommand>
{
    public DuplicateCardCommandValidator()
    {
    }
}
