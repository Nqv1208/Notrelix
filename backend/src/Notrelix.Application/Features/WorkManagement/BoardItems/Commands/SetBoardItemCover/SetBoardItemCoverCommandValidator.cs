namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemCover;

public class SetBoardItemCoverCommandValidator : AbstractValidator<SetBoardItemCoverCommand>
{
    public SetBoardItemCoverCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.Color).Must(ItemCover.IsColor).When(x => x.Color is not null).WithMessage("Unknown cover colour.");
        RuleFor(x => x.Style).Must(ItemCover.IsStyle).When(x => x.Style is not null).WithMessage("Cover style is strip or full.");
    }
}
