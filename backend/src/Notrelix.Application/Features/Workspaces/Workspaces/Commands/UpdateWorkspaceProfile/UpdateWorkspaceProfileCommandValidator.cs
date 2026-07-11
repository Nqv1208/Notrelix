namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;

public class UpdateWorkspaceProfileCommandValidator : AbstractValidator<UpdateWorkspaceProfileCommand>
{
    public UpdateWorkspaceProfileCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1024);
    }
}
