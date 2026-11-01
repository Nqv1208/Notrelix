namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.SetChecklistItemDue;

public class SetChecklistItemDueCommandValidator : AbstractValidator<SetChecklistItemDueCommand>
{
    public SetChecklistItemDueCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
