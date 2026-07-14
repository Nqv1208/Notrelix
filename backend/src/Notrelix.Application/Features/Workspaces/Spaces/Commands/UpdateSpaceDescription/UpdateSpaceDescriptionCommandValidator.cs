namespace Notrelix.Application.Features.Workspaces.Spaces.Commands.UpdateSpaceDescription;

public class UpdateSpaceDescriptionCommandValidator : AbstractValidator<UpdateSpaceDescriptionCommand>
{
    public UpdateSpaceDescriptionCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
