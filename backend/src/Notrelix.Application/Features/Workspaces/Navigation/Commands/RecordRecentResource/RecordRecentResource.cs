using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;

/// <summary>
/// WX-BE-001: the current member records that they opened a Board or Doc.
/// Upserts the entry's last-opened time and trims the member's recents to the
/// newest <see cref="WorkspaceNavigationRules.RecentRetentionLimit"/>.
/// </summary>
[IdempotencyOperation("workspaces.navigation.record-recent-resource.v1")]
public record RecordRecentResourceCommand(
    Guid WorkspaceId,
    string Kind,
    Guid ResourceId
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class RecordRecentResourceCommandHandler : IRequestHandler<RecordRecentResourceCommand, Result>
{
    private readonly IWorkspaceNavigationStore _store;
    private readonly IResourceLocator _resourceLocator;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordRecentResourceCommandHandler(
        IWorkspaceNavigationStore store,
        IResourceLocator resourceLocator,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _store = store;
        _resourceLocator = resourceLocator;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RecordRecentResourceCommand request, CancellationToken ct)
    {
        await NavigationResourceReference.EnsureInWorkspaceAsync(
            _resourceLocator, _requestContext, request.WorkspaceId, request.Kind, request.ResourceId, ct);

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        await _store.LockMemberNavigationAsync(request.WorkspaceId, userId, ct);

        var recents = (await _store.LoadRecentsAsync(request.WorkspaceId, userId, ct)).ToList();
        var existing = recents.FirstOrDefault(r =>
            string.Equals(r.ResourceKind, request.Kind, StringComparison.Ordinal) && r.ResourceId == request.ResourceId);

        if (existing is not null)
        {
            existing.RecordOpen(now);
        }
        else
        {
            var recent = RecentResource.Create(
                _requestContext.RequireAccountId(),
                request.WorkspaceId,
                userId,
                request.Kind,
                request.ResourceId,
                now);

            _store.AddRecent(recent);
            recents.Add(recent);
        }

        var evicted = WorkspaceNavigationRules.SelectRecentsToEvict(recents);
        if (evicted.Count > 0)
            _store.RemoveRecents(evicted.ToList());

        return Result.Success();
    }
}
