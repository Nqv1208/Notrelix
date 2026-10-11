namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.MoveSpace;

public class MoveSpaceCommandValidator : AbstractValidator<MoveSpaceCommand>
{
    public MoveSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.NewParentSpaceId).NotEqual(Guid.Empty).When(x => x.NewParentSpaceId.HasValue);
        RuleFor(x => x.PreviousSiblingId).NotEqual(Guid.Empty).When(x => x.PreviousSiblingId.HasValue);
        RuleFor(x => x.NextSiblingId).NotEqual(Guid.Empty).When(x => x.NextSiblingId.HasValue);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
