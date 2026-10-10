namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;

public class MoveBoardItemCommandValidator : AbstractValidator<MoveBoardItemCommand>
{
    public MoveBoardItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(v => v.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");

        RuleFor(v => v.NewGroupId)
            .NotEmpty().WithMessage("NewGroupId is required.");
    }
}
