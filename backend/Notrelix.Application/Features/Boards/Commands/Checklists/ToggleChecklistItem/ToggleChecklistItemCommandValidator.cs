using FluentValidation;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.ToggleChecklistItem;

public class ToggleChecklistItemCommandValidator : AbstractValidator<ToggleChecklistItemCommand>
{
    public ToggleChecklistItemCommandValidator()
    {
    }
}
