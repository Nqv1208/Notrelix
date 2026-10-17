namespace Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;

public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => Enum.GetNames<TeamMemberRole>().Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be Lead or Member.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
