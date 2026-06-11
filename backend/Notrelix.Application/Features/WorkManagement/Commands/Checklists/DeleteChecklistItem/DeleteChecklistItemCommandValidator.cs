using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.DeleteChecklistItem;

public class DeleteChecklistItemCommandValidator : AbstractValidator<DeleteChecklistItemCommand>
{
    public DeleteChecklistItemCommandValidator()
    {
    }
}
