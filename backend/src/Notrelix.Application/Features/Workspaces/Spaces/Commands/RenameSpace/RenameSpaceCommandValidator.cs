namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.RenameSpace;

public class RenameSpaceCommandValidator : AbstractValidator<RenameSpaceCommand>
{
    public RenameSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}
