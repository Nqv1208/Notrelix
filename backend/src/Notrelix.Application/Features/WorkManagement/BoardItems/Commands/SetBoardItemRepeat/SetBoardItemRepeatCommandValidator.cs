namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemRepeat;

public class SetBoardItemRepeatCommandValidator : AbstractValidator<SetBoardItemRepeatCommand>
{
    public SetBoardItemRepeatCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.Rule).Must(ItemRepeat.IsRule).When(x => x.Rule is not null).WithMessage("Unknown repeat rule.");
    }
}
