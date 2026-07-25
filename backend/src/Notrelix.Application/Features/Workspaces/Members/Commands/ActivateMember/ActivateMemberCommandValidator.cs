namespace Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;

public class ActivateMemberCommandValidator : AbstractValidator<ActivateMemberCommand>
{
    public ActivateMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
