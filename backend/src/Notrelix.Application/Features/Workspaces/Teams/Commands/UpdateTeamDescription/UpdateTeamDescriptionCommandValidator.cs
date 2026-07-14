namespace Notrelix.Application.Features.Workspaces.Teams.Commands.UpdateTeamDescription;

public class UpdateTeamDescriptionCommandValidator : AbstractValidator<UpdateTeamDescriptionCommand>
{
    public UpdateTeamDescriptionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
    }
}
