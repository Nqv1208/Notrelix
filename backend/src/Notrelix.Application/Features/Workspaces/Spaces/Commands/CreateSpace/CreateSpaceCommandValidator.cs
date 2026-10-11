namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;

public class CreateSpaceCommandValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Visibility)
            .Must(v => Enum.TryParse<SpaceVisibility>(v, ignoreCase: true, out _))
            .When(x => x.Visibility is not null)
            .WithMessage("Visibility must be Private or Workspace.");
        RuleFor(x => x.SpaceType)
            .Must(t => Enum.TryParse<SpaceType>(t, ignoreCase: true, out _))
            .When(x => x.SpaceType is not null)
            .WithMessage("Space type must be Folder, Portfolio, Department or ProjectArea.");
        RuleFor(x => x.ParentSpaceId).NotEqual(Guid.Empty).When(x => x.ParentSpaceId.HasValue);
        RuleFor(x => x.PreviousSiblingId).NotEqual(Guid.Empty).When(x => x.PreviousSiblingId.HasValue);
        RuleFor(x => x.NextSiblingId).NotEqual(Guid.Empty).When(x => x.NextSiblingId.HasValue);
    }
}
