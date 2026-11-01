namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemReminder;

public class SetBoardItemReminderCommandValidator : AbstractValidator<SetBoardItemReminderCommand>
{
    public SetBoardItemReminderCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.OffsetMinutes).Must(ItemReminder.IsOffset).WithMessage("Unknown reminder offset.");
    }
}
