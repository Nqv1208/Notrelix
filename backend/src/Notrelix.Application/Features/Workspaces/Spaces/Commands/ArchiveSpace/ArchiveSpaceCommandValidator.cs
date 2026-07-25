namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;

public class ArchiveSpaceCommandValidator : AbstractValidator<ArchiveSpaceCommand>
{
    public ArchiveSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
