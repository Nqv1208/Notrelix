using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UpdateCardStatusCommandValidator : AbstractValidator<UpdateBoardItemStatusCommand>
{
    public UpdateCardStatusCommandValidator()
    {
    }
}
