namespace Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

public class ReorderBlocksCommandValidator : AbstractValidator<ReorderBlocksCommand>
{
    public const int MaxItems = 500;

    public ReorderBlocksCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotNull()
            .Must(items => items.Count is > 0 and <= MaxItems)
            .WithMessage($"A reorder holds 1 to {MaxItems} blocks.")
            .Must(items => items.Select(item => item.BlockId).Distinct().Count() == items.Count)
            .WithMessage("Each block appears once in a reorder.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.BlockId).NotEmpty();
            item.RuleFor(x => x.ExpectedVersion)
                .NotNull().GreaterThan(0)
                .WithMessage("expectedVersion is required and must be the version of the block you read.");
        });
    }
}
