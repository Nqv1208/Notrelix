using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Observability;

internal sealed class OutboxDiagnosticsService : IOutboxDiagnosticsService
{
    private readonly ApplicationDbContext _context;

    public OutboxDiagnosticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OutboxStatsResult> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _context.Set<OutboxMessage>()
            .GroupBy(m => m.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = counts.Sum(c => c.count);
        var oldestPending = await _context.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Select(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new OutboxStatsResult(
            total,
            counts.ToDictionary(c => c.status, c => c.count),
            oldestPending);
    }

    public async Task<List<OutboxMessageResult>> GetPendingAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxStatus.Pending || m.Status == OutboxStatus.Processing)
            .OrderBy(m => m.CreatedAt)
            .Take(Math.Min(limit, 200))
            .Select(m => new OutboxMessageResult(
                m.Id,
                m.MessageName,
                m.MessageType.ToString(),
                m.Status.ToString(),
                m.RetryCount,
                m.CreatedAt,
                m.NextAttemptAt,
                m.ProcessingStartedAt,
                m.WorkspaceId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OutboxMessageResult>> GetFailedAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxStatus.Failed || m.Status == OutboxStatus.DeadLetter)
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Min(limit, 200))
            .Select(m => new OutboxMessageResult(
                m.Id,
                m.MessageName,
                m.MessageType.ToString(),
                m.Status.ToString(),
                m.RetryCount,
                m.CreatedAt,
                m.NextAttemptAt,
                m.ProcessingStartedAt,
                m.WorkspaceId))
            .ToListAsync(cancellationToken);
    }

    public async Task<OutboxMessageDetailResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<OutboxMessage>()
            .Where(m => m.Id == id)
            .Select(m => new OutboxMessageDetailResult(
                m.Id,
                m.EventId,
                m.SourceEventId,
                m.MessageName,
                m.SchemaVersion,
                m.MessageType.ToString(),
                m.EventVersion,
                m.Status.ToString(),
                m.RetryCount,
                m.MaxRetries,
                m.CreatedAt,
                m.NextAttemptAt,
                m.ProcessingStartedAt,
                m.ProcessedAt,
                m.Error,
                m.WorkspaceId,
                m.ActorUserId,
                m.CorrelationId,
                m.CausationId,
                m.PayloadJson))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
