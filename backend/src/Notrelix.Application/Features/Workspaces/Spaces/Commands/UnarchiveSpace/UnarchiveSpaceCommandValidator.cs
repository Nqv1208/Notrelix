namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.UnarchiveSpace;

public class UnarchiveSpaceCommandValidator : AbstractValidator<UnarchiveSpaceCommand>
{
    public UnarchiveSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
