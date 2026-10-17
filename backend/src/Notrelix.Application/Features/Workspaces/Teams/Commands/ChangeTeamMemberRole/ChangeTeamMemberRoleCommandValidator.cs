namespace Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;

public class ChangeTeamMemberRoleCommandValidator : AbstractValidator<ChangeTeamMemberRoleCommand>
{
    public ChangeTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole)
            .Must(r => Enum.GetNames<TeamMemberRole>().Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be Lead or Member.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
