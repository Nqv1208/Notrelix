using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class UpdateBoardColumnCommandValidator : AbstractValidator<UpdateBoardFieldCommand>
{
    public UpdateBoardColumnCommandValidator()
    {
    }
}
