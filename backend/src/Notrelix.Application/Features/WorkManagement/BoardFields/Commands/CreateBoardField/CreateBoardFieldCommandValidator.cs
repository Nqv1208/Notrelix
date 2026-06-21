using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

public class CreateBoardColumnCommandValidator : AbstractValidator<CreateBoardFieldCommand>
{
    public CreateBoardColumnCommandValidator()
    {
    }
}
