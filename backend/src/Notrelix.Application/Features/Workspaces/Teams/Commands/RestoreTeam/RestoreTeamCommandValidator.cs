namespace Notrelix.Application.Features.Workspaces.Teams.Commands.RestoreTeam;

public class RestoreTeamCommandValidator : AbstractValidator<RestoreTeamCommand>
{
    public RestoreTeamCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}
