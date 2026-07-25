namespace Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;

public class SuspendMemberCommandValidator : AbstractValidator<SuspendMemberCommand>
{
    public SuspendMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
