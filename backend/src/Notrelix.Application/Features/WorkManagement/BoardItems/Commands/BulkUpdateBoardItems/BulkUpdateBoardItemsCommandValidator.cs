namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.BulkUpdateBoardItems;

public class BulkUpdateBoardItemsCommandValidator : AbstractValidator<BulkUpdateBoardItemsCommand>
{
    public BulkUpdateBoardItemsCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Operation).Must(op => BulkBoardItemOperations.All.Contains(op))
            .WithMessage("Operation must be one of: " + string.Join(", ", BulkBoardItemOperations.All) + ".");
        RuleFor(x => x.ItemIds)
            .NotEmpty()
            .Must(ids => ids.Count <= BulkBoardItemOperations.MaxItems)
            .WithMessage($"At most {BulkBoardItemOperations.MaxItems} items can change at once.");
        RuleForEach(x => x.ItemIds).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().MaximumLength(200)
            .When(x => x.Operation == BulkBoardItemOperations.SetStatus);
        RuleFor(x => x.DueDate).NotNull()
            .When(x => x.Operation == BulkBoardItemOperations.SetDueDate);
        RuleFor(x => x.Priority).NotEmpty().MaximumLength(200)
            .When(x => x.Operation == BulkBoardItemOperations.SetPriority);
        RuleFor(x => x.AssigneeUserId).NotNull().NotEqual(Guid.Empty)
            .When(x => x.Operation == BulkBoardItemOperations.Assign);
    }
}
