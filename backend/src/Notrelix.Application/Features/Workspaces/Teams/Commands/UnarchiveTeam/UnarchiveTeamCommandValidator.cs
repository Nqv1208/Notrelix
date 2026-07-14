namespace Notrelix.Application.Features.Workspaces.Teams.Commands.UnarchiveTeam;

public class UnarchiveTeamCommandValidator : AbstractValidator<UnarchiveTeamCommand>
{
    public UnarchiveTeamCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}
