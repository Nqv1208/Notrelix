using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Governance.AccessChanges;

namespace Notrelix.Application.Features.Governance.AccessChanges;

/// <summary>
/// Adds one <see cref="ResourceAccessChange"/> to the request's unit of work; the pipeline owns the
/// commit, so a rolled-back change leaves no log line.
/// </summary>
public sealed class ResourceAccessChangeRecorder : IResourceAccessChangeRecorder
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public ResourceAccessChangeRecorder(IGovernanceDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public Task RecordAsync(ResourceAccessChangeRecord change, CancellationToken cancellationToken)
    {
        var kind = change.Change switch
        {
            AccessChangeKinds.Added => ResourceAccessChangeKind.Added,
            AccessChangeKinds.Changed => ResourceAccessChangeKind.Changed,
            AccessChangeKinds.Removed => ResourceAccessChangeKind.Removed,
            AccessChangeKinds.MadePrivate => ResourceAccessChangeKind.MadePrivate,
            AccessChangeKinds.VisibilityChanged => ResourceAccessChangeKind.VisibilityChanged,
            _ => throw new ArgumentOutOfRangeException(nameof(change), change.Change, "Unknown access change kind."),
        };

        _context.ResourceAccessChanges.Add(ResourceAccessChange.Record(
            _requestContext.RequireAccountId(),
            _requestContext.RequireWorkspaceId(),
            ResourceKind.Create(change.ResourceKind),
            change.ResourceId,
            kind,
            _requestContext.UserId,
            change.SubjectType,
            change.SubjectId,
            change.FromLevel,
            change.ToLevel,
            _clock.UtcNow));
        return Task.CompletedTask;
    }
}
