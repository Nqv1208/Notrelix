namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;

public class GetSpaceMembersQueryValidator : AbstractValidator<GetSpaceMembersQuery>
{
    public GetSpaceMembersQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
