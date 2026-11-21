namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ChangeWorkspaceSlug;

public class ChangeWorkspaceSlugCommandValidator : AbstractValidator<ChangeWorkspaceSlugCommand>
{
    public ChangeWorkspaceSlugCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        // The address rule (length, format, reserved words) is the Domain's; here the value only has to exist.
        RuleFor(x => x.Slug).NotEmpty();
    }
}
