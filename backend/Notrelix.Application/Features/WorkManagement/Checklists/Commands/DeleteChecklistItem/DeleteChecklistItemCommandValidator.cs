using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;

public class DeleteChecklistItemCommandValidator : AbstractValidator<DeleteChecklistItemCommand>
{
    public DeleteChecklistItemCommandValidator()
    {
    }
}
