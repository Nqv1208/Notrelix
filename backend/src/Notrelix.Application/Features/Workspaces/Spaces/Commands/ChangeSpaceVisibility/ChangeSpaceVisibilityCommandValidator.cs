namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceVisibility;

public class ChangeSpaceVisibilityCommandValidator : AbstractValidator<ChangeSpaceVisibilityCommand>
{
    public ChangeSpaceVisibilityCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Visibility).NotEmpty();
    }
}
