using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UpdateCardFieldValuesCommandValidator : AbstractValidator<UpdateBoardItemFieldValuesCommand>
{
    public UpdateCardFieldValuesCommandValidator()
    {
    }
}
