using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Operators.Public.Audit;
using Notrelix.Application.Features.Operators.Public.Principal;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Features.Accounts.Operator;

public sealed record OperatorAccountDto(Guid Id, string Name, string Slug, string Status, long Version, DateTimeOffset CreatedAt);

public sealed record OperatorAccountPageDto(IReadOnlyList<OperatorAccountDto> Items, int Total);

public sealed record OperatorAccountDetailDto(OperatorAccountDto Account, int ActiveMembers, int Workspaces);

/// <summary>
/// What the platform operator may do to an Account's lifecycle: find it, see its size, suspend it, lift the
/// suspension. The operator sees the Account's identity and status and two counts, never its content. Every
/// change is audited with the reason and the status before and after, in the same save.
/// </summary>
public sealed class OperatorAccountService
{
    private const int MaxPage = 100;

    private readonly IAccountDbContext _context;
    private readonly IOperatorAccountSummaryReader _summary;
    private readonly IOperatorAuditTrail _audit;
    private readonly IOperatorChangeCommit _commit;
    private readonly ICurrentOperator _operator;
    private readonly IDateTimeProvider _clock;

    public OperatorAccountService(
        IAccountDbContext context,
        IOperatorAccountSummaryReader summary,
        IOperatorAuditTrail audit,
        IOperatorChangeCommit commit,
        ICurrentOperator currentOperator,
        IDateTimeProvider clock)
    {
        _context = context;
        _summary = summary;
        _audit = audit;
        _commit = commit;
        _operator = currentOperator;
        _clock = clock;
    }

    public async Task<OperatorAccountPageDto> ListAsync(string? search, int skip, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, MaxPage);
        skip = Math.Max(0, skip);
        var query = _context.Accounts.AsNoTracking().Where(a => a.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(a => a.Name.ToLower().Contains(term) || a.Slug.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(a => a.Name).ThenBy(a => a.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return new OperatorAccountPageDto(rows.Select(ToDto).ToList(), total);
    }

    public async Task<OperatorAccountDetailDto> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await FindAsync(accountId, track: false, cancellationToken);
        var counts = await _summary.GetAsync(accountId, cancellationToken);
        return new OperatorAccountDetailDto(ToDto(account), counts.ActiveMembers, counts.Workspaces);
    }

    public Task<OperatorAccountDto> SuspendAsync(Guid accountId, string? reason, long expectedVersion, CancellationToken cancellationToken) =>
        ChangeAsync(accountId, reason, expectedVersion, "account.suspend", AccountStatus.Suspended, cancellationToken);

    public Task<OperatorAccountDto> LiftSuspensionAsync(Guid accountId, string? reason, long expectedVersion, CancellationToken cancellationToken) =>
        ChangeAsync(accountId, reason, expectedVersion, "account.lift-suspension", AccountStatus.Active, cancellationToken);

    private async Task<OperatorAccountDto> ChangeAsync(
        Guid accountId, string? reason, long expectedVersion, string action, AccountStatus target, CancellationToken cancellationToken)
    {
        if (expectedVersion <= 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("ExpectedVersion must be a positive value.");

        var account = await FindAsync(accountId, track: true, cancellationToken);
        if (account.Version != expectedVersion)
            throw new ConflictException("The account was changed by someone else. Reload and retry.");

        var before = new { status = account.Status.ToString(), version = account.Version };
        var now = _clock.UtcNow;
        if (target == AccountStatus.Suspended)
            account.Suspend(_operator.OperatorId, now, reason?.Trim());
        else
            account.Activate(_operator.OperatorId, now);

        // Already in the requested state: nothing changed, so nothing is audited and the version stays.
        if (account.Version == expectedVersion)
            return ToDto(account);

        _audit.Record(new OperatorAuditRecord(
            action, "account", account.Id, account.Id, reason ?? string.Empty,
            before, new { status = account.Status.ToString(), version = account.Version }));
        await _commit.CommitAsync(cancellationToken);
        return ToDto(account);
    }

    private async Task<Account> FindAsync(Guid accountId, bool track, CancellationToken cancellationToken)
    {
        var query = track ? _context.Accounts : _context.Accounts.AsNoTracking();
        return await query.FirstOrDefaultAsync(a => a.Id == accountId && a.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);
    }

    private static OperatorAccountDto ToDto(Account a) => new(a.Id, a.Name, a.Slug, a.Status.ToString(), a.Version, a.CreatedAt);
}
