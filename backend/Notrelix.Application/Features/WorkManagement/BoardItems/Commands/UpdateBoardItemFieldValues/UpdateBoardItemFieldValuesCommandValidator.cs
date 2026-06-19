using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;

public class UpdateCardFieldValuesCommandValidator : AbstractValidator<UpdateBoardItemFieldValuesCommand>
{
    public UpdateCardFieldValuesCommandValidator()
    {
    }
}
