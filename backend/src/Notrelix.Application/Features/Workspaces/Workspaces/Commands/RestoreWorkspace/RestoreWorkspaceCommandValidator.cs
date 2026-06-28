using FluentValidation;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;

public class RestoreWorkspaceCommandValidator : AbstractValidator<RestoreWorkspaceCommand>
{
    public RestoreWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
