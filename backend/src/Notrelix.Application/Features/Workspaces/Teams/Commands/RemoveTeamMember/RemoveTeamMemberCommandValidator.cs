namespace Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
