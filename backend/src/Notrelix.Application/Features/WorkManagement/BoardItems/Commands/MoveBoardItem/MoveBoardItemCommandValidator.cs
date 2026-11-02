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

        // The lane's field and option travel together: a drop names both or neither.
        RuleFor(v => v.LaneOptionId)
            .NotNull().When(v => v.LaneFieldId is not null)
            .WithMessage("LaneOptionId is required with LaneFieldId.");
        RuleFor(v => v.LaneFieldId)
            .NotNull().When(v => v.LaneOptionId is not null)
            .WithMessage("LaneFieldId is required with LaneOptionId.");
    }
}
