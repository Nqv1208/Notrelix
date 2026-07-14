namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.RestoreSpace;

public class RestoreSpaceCommandValidator : AbstractValidator<RestoreSpaceCommand>
{
    public RestoreSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
