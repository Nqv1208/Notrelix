namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;

public class GetSpaceTreeQueryValidator : AbstractValidator<GetSpaceTreeQuery>
{
    public GetSpaceTreeQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
