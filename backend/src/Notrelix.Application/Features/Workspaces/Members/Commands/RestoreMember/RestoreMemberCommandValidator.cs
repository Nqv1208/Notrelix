namespace Notrelix.Application.Features.Workspaces.Members.Commands.RestoreMember;

public class RestoreMemberCommandValidator : AbstractValidator<RestoreMemberCommand>
{
    public RestoreMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
