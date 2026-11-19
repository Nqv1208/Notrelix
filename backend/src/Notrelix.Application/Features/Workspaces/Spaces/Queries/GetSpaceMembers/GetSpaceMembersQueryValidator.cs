using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;

public class GetSpaceMembersQueryValidator : AbstractValidator<GetSpaceMembersQuery>
{
    public GetSpaceMembersQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, SpaceIdCursor.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => SpaceIdCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid space member list cursor.");
        RuleFor(x => x.Role)
            .Must(r => Enum.GetNames<SpaceRole>().Contains(r, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.Role))
            .WithMessage("Role must be Admin, Member or Viewer.");
        RuleFor(x => x.Kind)
            .Must(k => Enum.GetNames<SpaceMemberSubjectType>().Contains(k, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.Kind))
            .WithMessage("Kind must be User or Team.");
    }
}
