
namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

public sealed class CreateShareLinkCommandValidator : AbstractValidator<CreateShareLinkCommand>
{
    public CreateShareLinkCommandValidator()
    {
        RuleFor(x => x.ResourceKind)
            .Must(kind => ResourceKind.TryCreate(kind, out _))
            .WithMessage("'ResourceKind' must be a canonical resource kind (context.resource), e.g. 'work-management.board'.");

        // A share link grants Viewer or Commenter only, never Editor or above (Lead decision L5).
        RuleFor(x => x.Level)
            .Must(level => Enum.TryParse<PermissionLevel>(level, true, out var parsed) &&
                           parsed is PermissionLevel.Viewer or PermissionLevel.Commenter)
            .WithMessage("'Level' must be 'Viewer' or 'Commenter'. A share link never grants Editor or above.");

        // Creation requires an expiry (Lead decision L5); the workspace maximum is an owner question.
        RuleFor(x => x.ExpiresAt)
            .NotNull()
            .WithMessage("'ExpiresAt' is required: a share link needs an expiry.");
    }
}
