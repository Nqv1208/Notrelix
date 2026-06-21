using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

public class UpdateBoardColumnCommandValidator : AbstractValidator<UpdateBoardFieldCommand>
{
    public UpdateBoardColumnCommandValidator()
    {
    }
}
