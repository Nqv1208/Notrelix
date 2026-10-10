using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;

public class PinResourceCommandValidator : AbstractValidator<PinResourceCommand>
{
    public PinResourceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(NavigationResourceKinds.IsSupported)
            .WithMessage($"Kind must be one of: {string.Join(", ", NavigationResourceKinds.All)}.");
    }
}
