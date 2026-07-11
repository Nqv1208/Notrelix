namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;

public class ArchiveWorkspaceCommandValidator : AbstractValidator<ArchiveWorkspaceCommand>
{
    public ArchiveWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}
