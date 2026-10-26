namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageBacklinks;

public class GetPageBacklinksQueryValidator : AbstractValidator<GetPageBacklinksQuery>
{
    public GetPageBacklinksQueryValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}
