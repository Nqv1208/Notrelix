namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;

public class DeleteChecklistItemCommandValidator : AbstractValidator<DeleteChecklistItemCommand>
{
    public DeleteChecklistItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ItemId)
            .NotEmpty();
    }
}
