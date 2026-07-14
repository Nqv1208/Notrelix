namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;

public class UnarchiveWorkspaceCommandValidator : AbstractValidator<UnarchiveWorkspaceCommand>
{
    public UnarchiveWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
