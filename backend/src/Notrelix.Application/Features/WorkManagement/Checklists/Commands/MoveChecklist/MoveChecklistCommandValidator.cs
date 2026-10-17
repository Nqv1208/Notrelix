namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklist;

public class MoveChecklistCommandValidator : AbstractValidator<MoveChecklistCommand>
{
    public MoveChecklistCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
