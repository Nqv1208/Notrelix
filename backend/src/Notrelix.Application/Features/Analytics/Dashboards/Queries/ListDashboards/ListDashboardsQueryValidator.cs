using Notrelix.Application.Features.Analytics.Dashboards.Services;

namespace Notrelix.Application.Features.Analytics.Dashboards.Queries.ListDashboards;

public sealed class ListDashboardsQueryValidator : AbstractValidator<ListDashboardsQuery>
{
    public ListDashboardsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ListDashboardsQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => DashboardCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid dashboard cursor.");
    }
}
