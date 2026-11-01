namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;

public class UpdateWorkspaceProfileCommandValidator : AbstractValidator<UpdateWorkspaceProfileCommand>
{
    public UpdateWorkspaceProfileCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        // The name is optional (null leaves it unchanged); when sent it is a real name of at most 160.
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(1024);
    }
}
