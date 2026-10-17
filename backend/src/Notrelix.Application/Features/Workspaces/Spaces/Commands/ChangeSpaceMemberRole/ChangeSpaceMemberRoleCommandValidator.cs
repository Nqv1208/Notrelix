namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceMemberRole;

public class ChangeSpaceMemberRoleCommandValidator : AbstractValidator<ChangeSpaceMemberRoleCommand>
{
    public ChangeSpaceMemberRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.SubjectType)
            .Must(t => Enum.TryParse<SpaceMemberSubjectType>(t, ignoreCase: true, out _))
            .WithMessage("Subject type must be User or Team.");
        RuleFor(x => x.Role)
            .Must(r => Enum.TryParse<SpaceRole>(r, ignoreCase: true, out _))
            .WithMessage("Role must be Admin, Member or Viewer.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
