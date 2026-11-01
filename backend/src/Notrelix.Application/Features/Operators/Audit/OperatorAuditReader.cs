using Notrelix.Application.Features.Operators.Abstractions;

namespace Notrelix.Application.Features.Operators.Audit;

public sealed record OperatorAuditEntryDto(
    Guid Id, string OperatorEmail, string Action, string TargetKind, Guid TargetId, Guid? AccountId,
    string Reason, string? BeforeJson, string? AfterJson, DateTimeOffset OccurredAt);

/// <summary>Reads the operator audit trail, newest first, optionally for one account.</summary>
public sealed class OperatorAuditReader
{
    private const int MaxTake = 200;

    private readonly IOperatorsDbContext _context;

    public OperatorAuditReader(IOperatorsDbContext context) => _context = context;

    public async Task<IReadOnlyList<OperatorAuditEntryDto>> ListAsync(Guid? accountId, int take, CancellationToken cancellationToken)
    {
        var query = _context.OperatorAudit.AsNoTracking();
        if (accountId is { } id)
            query = query.Where(e => e.AccountId == id);

        return await query
            .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
            .Take(Math.Clamp(take, 1, MaxTake))
            .Join(_context.OperatorAccounts.AsNoTracking(), e => e.OperatorId, o => o.Id, (e, o) => new OperatorAuditEntryDto(
                e.Id, o.Email, e.Action, e.TargetKind, e.TargetId, e.AccountId, e.Reason, e.BeforeJson, e.AfterJson, e.OccurredAt))
            .ToListAsync(cancellationToken);
    }
}
