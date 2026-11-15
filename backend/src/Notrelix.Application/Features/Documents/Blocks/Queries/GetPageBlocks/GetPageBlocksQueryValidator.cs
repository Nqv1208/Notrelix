using Notrelix.Application.Features.Documents.Common;

namespace Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;

public class GetPageBlocksQueryValidator : AbstractValidator<GetPageBlocksQuery>
{
    public GetPageBlocksQueryValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, DocumentCursor.BlockPageSize)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(cursor => DocumentCursor.TryDecodePosition(cursor, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid block cursor.");
    }
}
