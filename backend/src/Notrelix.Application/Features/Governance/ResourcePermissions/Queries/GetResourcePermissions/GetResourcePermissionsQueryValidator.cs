namespace Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;

public sealed class GetResourcePermissionsQueryValidator : AbstractValidator<GetResourcePermissionsQuery>
{
    public GetResourcePermissionsQueryValidator()
    {
        RuleFor(x => x.ResourceKind)
            .Must(kind => ResourceKind.TryCreate(kind, out _))
            .WithMessage("'ResourceKind' must be a canonical resource kind (context.resource), e.g. 'work-management.board'.");
    }
}
