using FluentValidation;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;

public class BatchUpdateBlocksCommandValidator : AbstractValidator<BatchUpdateBlocksCommand>
{
    public BatchUpdateBlocksCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.Blocks)
            .NotNull()
            .Must(b => b.Count > 0);
    }
}
