using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public class DeleteBoardColumnCommandValidator : AbstractValidator<DeleteBoardFieldCommand>
{
    public DeleteBoardColumnCommandValidator()
    {
    }
}
