namespace Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;

public class GetTrashedPagesQueryValidator : AbstractValidator<GetTrashedPagesQuery>
{
    public GetTrashedPagesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
