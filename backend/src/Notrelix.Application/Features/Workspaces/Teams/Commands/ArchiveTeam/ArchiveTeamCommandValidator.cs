namespace Notrelix.Application.Features.Workspaces.Teams.Commands.ArchiveTeam;

public class ArchiveTeamCommandValidator : AbstractValidator<ArchiveTeamCommand>
{
    public ArchiveTeamCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}
