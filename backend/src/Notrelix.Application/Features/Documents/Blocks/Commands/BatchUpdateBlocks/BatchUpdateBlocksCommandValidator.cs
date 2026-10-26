namespace Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;

public class BatchUpdateBlocksCommandValidator : AbstractValidator<BatchUpdateBlocksCommand>
{
    public BatchUpdateBlocksCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.Blocks)
            .NotNull()
            .Must(b => b.Count > 0).WithMessage("A batch needs at least one block.")
            .Must(b => b.Count <= BatchBlockOperations.MaxOperations)
            .WithMessage($"A batch holds at most {BatchBlockOperations.MaxOperations} blocks.")
            .Must(b => b.Select(item => item.Id).Distinct().Count() == b.Count)
            .WithMessage("A block can appear only once in a batch.");

        RuleForEach(x => x.Blocks).ChildRules(item =>
        {
            item.RuleFor(b => b.Id).NotEmpty();
            item.RuleFor(b => b.Op)
                .Must(op => op is null
                    || string.Equals(op, BatchBlockOperations.Upsert, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(op, BatchBlockOperations.Delete, StringComparison.OrdinalIgnoreCase))
                .WithMessage("op must be 'upsert' or 'delete'.");
        });
    }
}
