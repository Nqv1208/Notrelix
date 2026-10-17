namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklistItem;

public class MoveChecklistItemCommandValidator : AbstractValidator<MoveChecklistItemCommand>
{
    public MoveChecklistItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
