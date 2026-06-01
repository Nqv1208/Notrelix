using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklistItem;

public class DeleteChecklistItemCommandValidator : AbstractValidator<DeleteChecklistItemCommand>
{
    public DeleteChecklistItemCommandValidator()
    {
    }
}
