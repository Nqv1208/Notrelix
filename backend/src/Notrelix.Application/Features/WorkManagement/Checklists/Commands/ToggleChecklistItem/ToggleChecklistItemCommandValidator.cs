namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;

public class ToggleChecklistItemCommandValidator : AbstractValidator<ToggleChecklistItemCommand>
{
    public ToggleChecklistItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ChecklistItemId)
            .NotEmpty();
    }
}
