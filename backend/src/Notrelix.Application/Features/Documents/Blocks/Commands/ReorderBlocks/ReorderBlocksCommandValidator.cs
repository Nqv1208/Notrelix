using FluentValidation;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

public class ReorderBlocksCommandValidator : AbstractValidator<ReorderBlocksCommand>
{
    public ReorderBlocksCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotNull()
            .Must(i => i.Count > 0);
    }
}
