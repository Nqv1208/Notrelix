namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

public class SetBoardItemDueDateCommandValidator : AbstractValidator<SetBoardItemDueDateCommand>
{
    public SetBoardItemDueDateCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardItemId)
            .NotEmpty();
    }
}
