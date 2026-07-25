namespace Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
