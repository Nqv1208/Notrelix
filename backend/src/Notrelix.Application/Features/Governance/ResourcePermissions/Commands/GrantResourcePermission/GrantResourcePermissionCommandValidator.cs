namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

public sealed class GrantResourcePermissionCommandValidator : AbstractValidator<GrantResourcePermissionCommand>
{
    public GrantResourcePermissionCommandValidator()
    {
        RuleFor(x => x.ResourceKind)
            .Must(kind => ResourceKind.TryCreate(kind, out _))
            .WithMessage("'ResourceKind' must be a canonical resource kind (context.resource), e.g. 'work-management.board'.");
    }
}
