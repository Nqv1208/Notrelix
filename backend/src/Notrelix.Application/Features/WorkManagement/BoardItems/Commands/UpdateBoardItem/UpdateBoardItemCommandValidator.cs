using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

public class UpdateBoardItemCommandValidator : AbstractValidator<UpdateBoardItemCommand>
{
    public UpdateBoardItemCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.DueDate).Null().When(x => x.ClearDueDate).WithMessage("Give a due date or clear it, not both.");
        RuleFor(x => x.StartDate).Null().When(x => x.ClearStartDate).WithMessage("Give a start date or clear it, not both.");
        RuleFor(x => x.DescriptionMd).MaximumLength(BoardItem.DescriptionMaxLength);
    }
}
