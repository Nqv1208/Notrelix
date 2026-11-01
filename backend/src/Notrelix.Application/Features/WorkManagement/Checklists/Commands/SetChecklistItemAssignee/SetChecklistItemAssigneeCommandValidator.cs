namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.SetChecklistItemAssignee;

public class SetChecklistItemAssigneeCommandValidator : AbstractValidator<SetChecklistItemAssigneeCommand>
{
    public SetChecklistItemAssigneeCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.AssigneeUserId!.Value).NotEmpty().When(x => x.AssigneeUserId is not null);
    }
}
