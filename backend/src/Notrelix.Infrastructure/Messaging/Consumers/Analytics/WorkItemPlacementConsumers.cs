using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Placements.Services;

namespace Notrelix.Infrastructure.Messaging.Consumers.Analytics;

/// <summary>
/// Analytics-owned placement projection consumers for Work facts. Thin
/// inbound adapters: they translate the producer event into the projection
/// update and let the Platform dedup filter own duplicate delivery. A fact
/// missing workspace scope is not projectable and is skipped. Consumers that
/// already carry every placement fact in the event project it directly; only
/// facts the payload genuinely lacks are resolved through the producer-owned
/// snapshot contract — never through foreign persistence. Ordering uses the
/// single producer-timestamp watermark.
/// </summary>
public sealed class BoardItemMovedPlacementConsumer
    : IConsumer<BoardItemMovedIntegrationEvent>
{
    private readonly WorkspaceWorkItemPlacementService _service;
    private readonly ICurrentTenantContext _tenant;
    private readonly ILogger<BoardItemMovedPlacementConsumer> _logger;

    public BoardItemMovedPlacementConsumer(
        WorkspaceWorkItemPlacementService service,
        ICurrentTenantContext tenant,
        ILogger<BoardItemMovedPlacementConsumer> logger)
    {
        _service = service;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BoardItemMovedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.WorkspaceId is null || msg.NewGroupId is null)
            return;

        // The moved event already carries every placement fact; no producer
        // snapshot read is required. Account scope falls back to the tenant
        // restored for this message by the Platform runtime.
        var accountId = msg.AccountId ?? _tenant.RequireAccountId();

        var applied = await _service.ApplyPlacementAsync(
            accountId,
            msg.WorkspaceId.Value,
            msg.ItemId,
            msg.BoardId,
            msg.NewGroupId.Value,
            isArchived: false,
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

        // The created payload carries no GroupId; fetch the current
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
            lastOccurredAt: msg.OccurredAt,
            context.CancellationToken);

        if (applied)
            _logger.LogDebug("Placement projected: archived item {ItemId}", msg.ItemId);
    }
}
