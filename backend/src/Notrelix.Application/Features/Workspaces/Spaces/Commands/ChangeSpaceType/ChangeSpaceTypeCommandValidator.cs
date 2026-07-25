namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceType;

public class ChangeSpaceTypeCommandValidator : AbstractValidator<ChangeSpaceTypeCommand>
{
    public ChangeSpaceTypeCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.SpaceType).NotEmpty();
    }
}
