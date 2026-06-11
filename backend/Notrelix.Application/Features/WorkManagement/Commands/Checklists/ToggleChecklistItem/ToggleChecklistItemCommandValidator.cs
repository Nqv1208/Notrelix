using FluentValidation;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.ToggleChecklistItem;

public class ToggleChecklistItemCommandValidator : AbstractValidator<ToggleChecklistItemCommand>
{
    public ToggleChecklistItemCommandValidator()
    {
    }
}
