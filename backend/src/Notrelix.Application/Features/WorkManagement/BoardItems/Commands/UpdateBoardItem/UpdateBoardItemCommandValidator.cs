using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

public class UpdateBoardItemCommandValidator : AbstractValidator<UpdateBoardItemCommand>
{
    public UpdateBoardItemCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
    }
}
