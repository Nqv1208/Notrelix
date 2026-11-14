using Notrelix.Application.Features.Documents.Common;

namespace Notrelix.Application.Features.Documents.Versions.Queries.ListPageVersions;

public class ListPageVersionsQueryValidator : AbstractValidator<ListPageVersionsQuery>
{
    public ListPageVersionsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, DocumentCursor.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(cursor => DocumentCursor.TryDecodeNumber(cursor, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid version cursor.");
    }
}
