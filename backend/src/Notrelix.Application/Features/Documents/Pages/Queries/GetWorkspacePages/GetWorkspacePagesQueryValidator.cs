using Notrelix.Application.Features.Documents.Common;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

public class GetWorkspacePagesQueryValidator : AbstractValidator<GetWorkspacePagesQuery>
{
    public GetWorkspacePagesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, DocumentCursor.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(cursor => DocumentCursor.TryDecodeId(cursor, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid page cursor.");
    }
}
