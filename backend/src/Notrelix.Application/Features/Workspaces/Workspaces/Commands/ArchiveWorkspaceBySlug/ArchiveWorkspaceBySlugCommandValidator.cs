namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspaceBySlug;

public class ArchiveWorkspaceBySlugCommandValidator : AbstractValidator<ArchiveWorkspaceBySlugCommand>
{
    public ArchiveWorkspaceBySlugCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(200);
    }
}
