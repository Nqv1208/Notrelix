using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UpdateCardCommandValidator : AbstractValidator<UpdateBoardItemCommand>
{
    public UpdateCardCommandValidator()
    {
    }
}
