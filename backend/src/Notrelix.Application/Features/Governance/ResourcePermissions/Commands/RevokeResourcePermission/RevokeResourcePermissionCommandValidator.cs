namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;

public sealed class RevokeResourcePermissionCommandValidator : AbstractValidator<RevokeResourcePermissionCommand>
{
    public RevokeResourcePermissionCommandValidator()
    {
        RuleFor(x => x.ResourceKind)
            .Must(kind => ResourceKind.TryCreate(kind, out _))
            .WithMessage("'ResourceKind' must be a canonical resource kind (context.resource), e.g. 'work-management.board'.");
    }
}
