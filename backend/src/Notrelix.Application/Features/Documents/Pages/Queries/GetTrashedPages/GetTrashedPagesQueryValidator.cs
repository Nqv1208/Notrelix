namespace Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;

public class GetTrashedPagesQueryValidator : AbstractValidator<GetTrashedPagesQuery>
{
    public GetTrashedPagesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetTrashedPagesQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(cursor => TrashCursor.TryDecode(cursor, out _, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid trash cursor.");
    }
}
