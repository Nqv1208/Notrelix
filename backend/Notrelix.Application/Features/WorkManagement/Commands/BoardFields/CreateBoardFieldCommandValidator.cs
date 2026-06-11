using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class CreateBoardColumnCommandValidator : AbstractValidator<CreateBoardFieldCommand>
{
    public CreateBoardColumnCommandValidator()
    {
    }
}
