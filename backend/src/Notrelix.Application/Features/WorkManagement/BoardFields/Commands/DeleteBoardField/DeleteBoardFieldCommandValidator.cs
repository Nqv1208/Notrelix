using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;

public class DeleteBoardColumnCommandValidator : AbstractValidator<DeleteBoardFieldCommand>
{
    public DeleteBoardColumnCommandValidator()
    {
    }
}
