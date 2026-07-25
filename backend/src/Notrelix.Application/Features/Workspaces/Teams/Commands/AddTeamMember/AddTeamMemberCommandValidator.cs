namespace Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;

public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
    }
}
