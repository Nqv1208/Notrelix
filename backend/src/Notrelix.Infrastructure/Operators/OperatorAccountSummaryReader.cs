using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Operators;

/// <summary>Reads the impact preview through the owner-defined function that returns two counts and nothing else.</summary>
internal sealed class OperatorAccountSummaryReader : IOperatorAccountSummaryReader
{
    private readonly ApplicationDbContext _context;

    public OperatorAccountSummaryReader(ApplicationDbContext context) => _context = context;

    public async Task<OperatorAccountSummary> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<SummaryRow>($"SELECT active_members, workspaces FROM operators.account_summary({accountId})")
            .ToListAsync(cancellationToken);
        var row = rows.SingleOrDefault();
        return row is null ? new OperatorAccountSummary(0, 0) : new OperatorAccountSummary(row.ActiveMembers, row.Workspaces);
    }

    private sealed class SummaryRow
    {
        public int ActiveMembers { get; set; }
        public int Workspaces { get; set; }
    }
}
