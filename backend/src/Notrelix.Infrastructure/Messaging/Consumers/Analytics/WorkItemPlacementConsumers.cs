using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Analytics.Placements.Services;
using Notrelix.Application.Features.WorkManagement.Public.Queries;

namespace Notrelix.Infrastructure.Messaging.Consumers.Analytics;

/// <summary>
/// Analytics-owned placement projection consumers for Work facts. Thin
/// inbound adapters: they translate the producer event into the projection
/// update and let the Platform dedup filter own duplicate delivery. A fact
/// missing workspace scope is not projectable and is skipped. Scope facts the
/// payload lacks are resolved through the producer-owned snapshot contract —
/// never through foreign persistence.
/// </summary>
public sealed class BoardItemMovedPlacementConsumer
    : IConsumer<BoardItemMovedIntegrationEvent>
{
    private readonly WorkspaceWorkItemPlacementService _service;
    private readonly IWorkItemProjectionSourceAdapter _projectionSource;
    private readonly ILogger<BoardItemMovedPlacementConsumer> _logger;

    public BoardItemMovedPlacementConsumer(
        WorkspaceWorkItemPlacementService service,
        IWorkItemProjectionSourceAdapter projectionSource,
        ILogger<BoardItemMovedPlacementConsumer> logger)
    {
        _service = service;
        _projectionSource = projectionSource;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BoardItemMovedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null || msg.NewGroupId is null)
            return;

        // The moved payload carries no account scope; resolve it through the
        // producer-owned snapshot. Revision guards on the envelope timestamp.
        var snapshot = await _projectionSource.GetItemPlacementAsync(
            msg.WorkspaceId.Value, msg.ItemId, context.CancellationToken);
        if (snapshot is null)
        {
            _logger.LogDebug("Moved item {ItemId} had no placement snapshot yet", msg.ItemId);
            return;
        }

        var applied = await _service.ApplyPlacementAsync(
            snapshot.AccountId,
            msg.WorkspaceId.Value,
            msg.ItemId,
            msg.BoardId,
            msg.NewGroupId.Value,
            isArchived: false,
            sourceRevision: msg.OccurredAt.UtcTicks,
            lastOccurredAt: msg.OccurredAt,
            context.CancellationToken);

        if (applied)
            _logger.LogDebug("Placement projected: moved item {ItemId}", msg.ItemId);
    }
}

public sealed class BoardItemCreatedPlacementConsumer
    : IConsumer<BoardItemCreatedIntegrationEvent>
{
    private readonly WorkspaceWorkItemPlacementService _service;
    private readonly IWorkItemProjectionSourceAdapter _projectionSource;
    private readonly ILogger<BoardItemCreatedPlacementConsumer> _logger;

    public BoardItemCreatedPlacementConsumer(
        WorkspaceWorkItemPlacementService service,
        IWorkItemProjectionSourceAdapter projectionSource,
        ILogger<BoardItemCreatedPlacementConsumer> logger)
    {
        _service = service;
        _projectionSource = projectionSource;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BoardItemCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null)
            return;

        // The created payload carries no GroupId/account; fetch the current
        // placement through the producer-owned snapshot contract.
        var snapshot = await _projectionSource.GetItemPlacementAsync(
            msg.WorkspaceId.Value, msg.ItemId, context.CancellationToken);

        if (snapshot is null)
        {
            _logger.LogDebug("Created item {ItemId} had no placement snapshot yet", msg.ItemId);
            return;
        }

        var applied = await _service.ApplyPlacementAsync(
            snapshot.AccountId,
            msg.WorkspaceId.Value,
            snapshot.ItemId,
            snapshot.BoardId,
            snapshot.GroupId,
            snapshot.IsArchived,
            snapshot.Revision,
            snapshot.LastOccurredAt,
            context.CancellationToken);

        if (applied)
            _logger.LogDebug("Placement projected: created item {ItemId}", msg.ItemId);
    }
}

public sealed class BoardItemArchivedPlacementConsumer
    : IConsumer<BoardItemArchivedIntegrationEvent>
{
    private readonly WorkspaceWorkItemPlacementService _service;
    private readonly ILogger<BoardItemArchivedPlacementConsumer> _logger;

    public BoardItemArchivedPlacementConsumer(
        WorkspaceWorkItemPlacementService service,
        ILogger<BoardItemArchivedPlacementConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BoardItemArchivedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null)
            return;

        // Retention semantics: archived items stay projected with the archive
        // flag rather than being deleted, so placement history remains visible.
        var applied = await _service.MarkArchivedAsync(
            msg.WorkspaceId.Value,
            msg.ItemId,
            sourceRevision: msg.OccurredAt.UtcTicks,
            lastOccurredAt: msg.OccurredAt,
            context.CancellationToken);

        if (applied)
            _logger.LogDebug("Placement projected: archived item {ItemId}", msg.ItemId);
    }
}

/// <summary>
/// Runtime adapter seam for the producer-owned projection source, so the
/// consumer never touches Work persistence. Infrastructure wires it to the
/// producer Public contract.
/// </summary>
public interface IWorkItemProjectionSourceAdapter
{
    Task<WorkItemPlacementSnapshot?> GetItemPlacementAsync(
        Guid workspaceId,
        Guid itemId,
        CancellationToken cancellationToken);
}
