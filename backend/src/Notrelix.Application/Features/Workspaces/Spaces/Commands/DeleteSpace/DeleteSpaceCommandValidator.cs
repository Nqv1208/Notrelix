namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;

public class DeleteSpaceCommandValidator : AbstractValidator<DeleteSpaceCommand>
{
    public DeleteSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
