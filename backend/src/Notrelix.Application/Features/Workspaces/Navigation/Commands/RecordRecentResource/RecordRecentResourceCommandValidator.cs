using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;

public class RecordRecentResourceCommandValidator : AbstractValidator<RecordRecentResourceCommand>
{
    public RecordRecentResourceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(NavigationResourceKinds.IsSupported)
            .WithMessage($"Kind must be one of: {string.Join(", ", NavigationResourceKinds.All)}.");
    }
}
