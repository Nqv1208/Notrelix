using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Operators.Public.Audit;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Operator;

public sealed record OperatorPlanLimitDto(string FeatureCode, bool IsUnlimited, int Limit);

public sealed record OperatorPlanDto(
    Guid Id, string FamilyKey, int Revision, string Name, string Tier, decimal PriceAmount, string Currency,
    string Period, string Status, long Version, IReadOnlyList<OperatorPlanLimitDto> Limits, DateTimeOffset CreatedAt);

public sealed record OperatorPlanRevisionInput(
    string FamilyKey, int Revision, string Name, string Tier, decimal PriceAmount, string Currency, string Period,
    IReadOnlyList<OperatorPlanLimitDto> Limits, string? Reason);

/// <summary>
/// The plan catalog as the platform operator edits it: revisions are created, never changed; an older revision
/// is deprecated (no longer sold) or archived (kept for the subscriptions that reference it). Nothing is ever
/// deleted. The newest active revision of the default family cannot be retired, because an account without a
/// subscription lands on it.
/// </summary>
public sealed class OperatorPlanCatalogService
{
    private readonly IBillingDbContext _context;
    private readonly IOperatorAuditTrail _audit;
    private readonly IOperatorChangeCommit _commit;
    private readonly IDateTimeProvider _clock;

    public OperatorPlanCatalogService(
        IBillingDbContext context, IOperatorAuditTrail audit, IOperatorChangeCommit commit, IDateTimeProvider clock)
    {
        _context = context;
        _audit = audit;
        _commit = commit;
        _clock = clock;
    }

    public async Task<IReadOnlyList<OperatorPlanDto>> ListAsync(CancellationToken cancellationToken)
    {
        var plans = await _context.Plans.AsNoTracking().Include(p => p.Limits).ToListAsync(cancellationToken);
        return plans.OrderBy(p => p.FamilyKey, StringComparer.Ordinal).ThenByDescending(p => p.Revision).Select(ToDto).ToList();
    }

    public async Task<OperatorPlanDto> CreateRevisionAsync(OperatorPlanRevisionInput input, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SubscriptionTier>(input.Tier, ignoreCase: true, out var tier) || !Enum.IsDefined(tier))
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"Tier must be one of: {string.Join(", ", Enum.GetNames<SubscriptionTier>())}.");
        if (!Enum.TryParse<BillingPeriod>(input.Period, ignoreCase: true, out var period) || !Enum.IsDefined(period))
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"Period must be one of: {string.Join(", ", Enum.GetNames<BillingPeriod>())}.");

        var limits = input.Limits ?? [];
        var unknown = limits.Select(l => l.FeatureCode?.Trim().ToUpperInvariant() ?? string.Empty)
            .FirstOrDefault(code => !BillingCapabilityCode.All.Contains(code));
        if (unknown is not null)
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"'{unknown}' is not a capability the catalog knows. Known: {string.Join(", ", BillingCapabilityCode.All)}.");

        var now = _clock.UtcNow;
        var plan = Plan.Create(input.FamilyKey, input.Revision, input.Name, tier, Money.Create(input.PriceAmount, input.Currency), period, now);
        foreach (var limit in limits)
            plan.AddLimit(FeatureCode.Create(limit.FeatureCode), limit.Limit, now, limit.IsUnlimited);

        var family = plan.FamilyKey;
        var revisions = await _context.Plans.AsNoTracking().Include(p => p.Limits)
            .Where(p => p.FamilyKey == family).ToListAsync(cancellationToken);
        var existing = revisions.FirstOrDefault(p => p.Revision == plan.Revision);
        if (existing is not null)
        {
            // Re-submitting the same revision is a no-op; the same number with other content is a conflict.
            return SameContent(existing, plan)
                ? ToDto(existing)
                : throw new ConflictException($"Revision {plan.Revision} of '{family}' already exists with other content. A revision is never changed: publish the next one.");
        }

        var next = (revisions.Count == 0 ? 0 : revisions.Max(p => p.Revision)) + 1;
        if (plan.Revision != next)
            throw new ConflictException($"The next revision of '{family}' is {next}.");

        _context.Plans.Add(plan);
        _audit.Record(new OperatorAuditRecord(
            "plan.create-revision", "plan", plan.Id, null, input.Reason ?? string.Empty, null, Snapshot(plan)));
        await _commit.CommitAsync(cancellationToken);
        return ToDto(plan);
    }

    public Task<OperatorPlanDto> DeprecateAsync(Guid planId, string? reason, long expectedVersion, CancellationToken cancellationToken) =>
        RetireAsync(planId, reason, expectedVersion, "plan.deprecate", PlanStatus.Deprecated, cancellationToken);

    public Task<OperatorPlanDto> ArchiveAsync(Guid planId, string? reason, long expectedVersion, CancellationToken cancellationToken) =>
        RetireAsync(planId, reason, expectedVersion, "plan.archive", PlanStatus.Archived, cancellationToken);

    private async Task<OperatorPlanDto> RetireAsync(
        Guid planId, string? reason, long expectedVersion, string action, PlanStatus target, CancellationToken cancellationToken)
    {
        if (expectedVersion <= 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("ExpectedVersion must be a positive value.");

        var plan = await _context.Plans.Include(p => p.Limits).FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), planId);
        if (plan.Version != expectedVersion)
            throw new ConflictException("The plan was changed by someone else. Reload and retry.");
        if (plan.Status == target)
            return ToDto(plan);

        if (plan.Status == PlanStatus.Active && plan.FamilyKey == BillingPlanCatalogReader.DefaultFamily)
        {
            var otherActive = await _context.Plans.AnyAsync(
                p => p.FamilyKey == plan.FamilyKey && p.Status == PlanStatus.Active && p.Id != plan.Id, cancellationToken);
            if (!otherActive)
                throw new ConflictException($"'{plan.FamilyKey}' is the default plan: publish a newer active revision before retiring this one.");
        }

        var before = Snapshot(plan);
        var now = _clock.UtcNow;
        if (target == PlanStatus.Deprecated)
        {
            if (plan.Status == PlanStatus.Archived)
                throw new ConflictException("An archived revision cannot be deprecated.");
            plan.Deprecate(now);
        }
        else
        {
            plan.Archive(now);
        }

        _audit.Record(new OperatorAuditRecord(action, "plan", plan.Id, null, reason ?? string.Empty, before, Snapshot(plan)));
        await _commit.CommitAsync(cancellationToken);
        return ToDto(plan);
    }

    private static bool SameContent(Plan a, Plan b) =>
        a.Name == b.Name && a.Tier == b.Tier && a.Price.Equals(b.Price) && a.Period == b.Period
        && a.Limits.Select(Key).OrderBy(x => x, StringComparer.Ordinal)
            .SequenceEqual(b.Limits.Select(Key).OrderBy(x => x, StringComparer.Ordinal));

    private static string Key(PlanLimit l) => $"{l.Feature.Code}:{(l.IsUnlimited ? "unlimited" : l.Limit.ToString())}";

    private static object Snapshot(Plan p) => new
    {
        family = p.FamilyKey,
        revision = p.Revision,
        status = p.Status.ToString(),
        tier = p.Tier.ToString(),
        price = p.Price.Amount,
        currency = p.Price.Currency,
        version = p.Version,
        limits = p.Limits.Select(Key).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
    };

    private static OperatorPlanDto ToDto(Plan p) => new(
        p.Id, p.FamilyKey, p.Revision, p.Name, p.Tier.ToString(), p.Price.Amount, p.Price.Currency, p.Period.ToString(),
        p.Status.ToString(), p.Version,
        p.Limits.OrderBy(l => l.Feature.Code, StringComparer.Ordinal)
            .Select(l => new OperatorPlanLimitDto(l.Feature.Code, l.IsUnlimited, l.Limit)).ToList(),
        p.CreatedAt);
}
