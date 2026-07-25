namespace Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;

public class ChangeTeamMemberRoleCommandValidator : AbstractValidator<ChangeTeamMemberRoleCommand>
{
    public ChangeTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).NotEmpty();
    }
}
