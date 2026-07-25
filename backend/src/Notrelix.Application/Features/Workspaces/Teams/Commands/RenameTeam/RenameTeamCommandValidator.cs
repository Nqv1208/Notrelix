namespace Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;

public class RenameTeamCommandValidator : AbstractValidator<RenameTeamCommand>
{
    public RenameTeamCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}
