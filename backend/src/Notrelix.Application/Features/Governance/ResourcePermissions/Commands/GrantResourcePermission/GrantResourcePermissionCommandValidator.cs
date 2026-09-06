namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

public sealed class GrantResourcePermissionCommandValidator : AbstractValidator<GrantResourcePermissionCommand>
{
    public GrantResourcePermissionCommandValidator()
    {
        RuleFor(x => x.ResourceKind)
            .Must(kind => ResourceKind.TryCreate(kind, out _))
            .WithMessage("'ResourceKind' must be a canonical resource kind (context.resource), e.g. 'work-management.board'.");

        RuleFor(x => x.SubjectType)
            .Must(subjectType => Enum.TryParse<PermissionSubjectType>(subjectType, true, out _))
            .WithMessage("'SubjectType' must be a canonical permission subject type, e.g. 'User'.");

        RuleFor(x => x.Level)
            .Must(level => Enum.TryParse<PermissionLevel>(level, true, out var parsed) && parsed > PermissionLevel.None)
            .WithMessage("'Level' must be a known permission level ('Viewer', 'Commenter', 'Editor', 'Manager' or 'Owner').");

        RuleFor(x => x.ExpiresAt)
            .Null()
            .WithMessage("Expiration is not supported for resource permissions.");
    }
}
