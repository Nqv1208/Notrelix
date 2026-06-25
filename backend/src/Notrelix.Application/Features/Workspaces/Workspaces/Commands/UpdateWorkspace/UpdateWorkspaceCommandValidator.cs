using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspace;

public class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
