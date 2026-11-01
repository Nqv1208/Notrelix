using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Plans;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Operators.Public.Audit;
using Notrelix.Application.Features.Operators.Public.Principal;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Features.Billing.Operator;

public sealed record OperatorCurrentPlanDto(Guid PlanId, string FamilyKey, int Revision, string Name);

public sealed record OperatorSubscriptionDto(
    Guid Id, Guid PlanId, string Status, string Tier, long Version, DateTimeOffset CurrentPeriodEnd);

public sealed record OperatorOverrideDto(
    Guid Id, string FeatureCode, bool IsUnlimited, int Limit, string Source, string Status, long Version,
    DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);

public sealed record OperatorAccountBillingDto(
    OperatorCurrentPlanDto CurrentPlan, OperatorSubscriptionDto? Subscription, IReadOnlyList<OperatorOverrideDto> Overrides);

public sealed record OperatorOverrideInput(
    string FeatureCode, bool IsUnlimited, int Limit, DateTimeOffset? ExpiresAt, string? Reason);

public sealed record OperatorAccountPlanDto(Guid AccountId, string FamilyKey, int Revision, string Name);

/// <summary>
/// What the platform operator may do to one account's commercial state: move it to a catalog revision, end its
/// subscription now, grant or revoke a manual entitlement override. The plan or override changes what the tenant
/// sees at once, through the same resolution every capability decision uses. Each change is audited with the
/// reason and the values before and after, in the same save as the change.
/// </summary>
public sealed class OperatorAccountBillingService
{
    private static readonly TimeSpan SubscriptionTerm = TimeSpan.FromDays(3650);

    private readonly IBillingDbContext _context;
    private readonly BillingPlanResolver _resolver;
    private readonly IAccountMembershipFacts _accounts;
    private readonly IOperatorAuditTrail _audit;
    private readonly IOperatorChangeCommit _commit;
    private readonly ICurrentOperator _operator;
    private readonly IDateTimeProvider _clock;

    public OperatorAccountBillingService(
        IBillingDbContext context,
        BillingPlanResolver resolver,
        IAccountMembershipFacts accounts,
        IOperatorAuditTrail audit,
        IOperatorChangeCommit commit,
        ICurrentOperator currentOperator,
        IDateTimeProvider clock)
    {
        _context = context;
        _resolver = resolver;
        _accounts = accounts;
        _audit = audit;
        _commit = commit;
        _operator = currentOperator;
        _clock = clock;
    }

    /// <summary>The plan each of the given accounts is on, for the tenant list.</summary>
    public async Task<IReadOnlyList<OperatorAccountPlanDto>> GetPlansAsync(
        IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        var result = new List<OperatorAccountPlanDto>();
        foreach (var id in accountIds.Distinct().Take(100))
        {
            var plan = await _resolver.ResolveAsync(id, cancellationToken);
            result.Add(new OperatorAccountPlanDto(id, plan.Code, plan.Revision, plan.Name));
        }

        return result;
    }

    public async Task<OperatorAccountBillingDto> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await RequireAccountAsync(accountId, cancellationToken);
        var plan = await _resolver.ResolveAsync(accountId, cancellationToken);
        var subscription = await LatestSubscriptionAsync(accountId, track: false, cancellationToken);
        var overrides = await _context.Entitlements.AsNoTracking()
            .Where(e => e.AccountId == accountId).OrderByDescending(e => e.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return new OperatorAccountBillingDto(
            new OperatorCurrentPlanDto(plan.PlanId, plan.Code, plan.Revision, plan.Name),
            subscription is null ? null : ToDto(subscription),
            overrides.Select(ToDto).ToList());
    }

    public async Task<OperatorAccountBillingDto> ChangePlanAsync(
        Guid accountId, Guid planId, string? reason, long? expectedVersion, CancellationToken cancellationToken)
    {
        await RequireAccountAsync(accountId, cancellationToken);
        var plan = await _context.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), planId);
        if (plan.Status != PlanStatus.Active)
            throw new ConflictException("Only an active plan revision can be assigned: this one is no longer sold.");

        var now = _clock.UtcNow;
        var current = await LatestSubscriptionAsync(accountId, track: true, cancellationToken);
        var live = current is { Status: SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Trialing } ? current : null;
        if (live is not null && expectedVersion is { } seen && live.Version != seen)
            throw new ConflictException("The subscription was changed by someone else. Reload and retry.");

        if (live is not null && live.PlanId == plan.Id && live.Tier == plan.Tier && live.CurrentPeriodEnd > now)
            return await GetAsync(accountId, cancellationToken);

        var before = new { subscriptionId = live?.Id, planId = live?.PlanId, tier = live?.Tier.ToString(), version = live?.Version };
        Subscription changed;
        if (live is null)
        {
            changed = Subscription.Create(accountId, plan.Id, plan.Tier, now, now.Add(SubscriptionTerm), _operator.OperatorId, now);
            _context.Subscriptions.Add(changed);
        }
        else
        {
            changed = live;
            changed.ChangePlan(plan.Id, plan.Tier, _operator.OperatorId, now);
            if (changed.CurrentPeriodEnd <= now)
                changed.Renew(now, now.Add(SubscriptionTerm), _operator.OperatorId, now);
        }

        _audit.Record(new OperatorAuditRecord(
            "subscription.change-plan", "subscription", changed.Id, accountId, reason ?? string.Empty,
            before, new { subscriptionId = changed.Id, planId = plan.Id, family = plan.FamilyKey, revision = plan.Revision, tier = plan.Tier.ToString() }));
        await _commit.CommitAsync(cancellationToken);
        return await GetAsync(accountId, cancellationToken);
    }

    public async Task<OperatorAccountBillingDto> TerminateSubscriptionAsync(
        Guid accountId, string? reason, long expectedVersion, CancellationToken cancellationToken)
    {
        if (expectedVersion <= 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("ExpectedVersion must be a positive value.");

        await RequireAccountAsync(accountId, cancellationToken);
        var subscription = await LatestSubscriptionAsync(accountId, track: true, cancellationToken)
            ?? throw new NotFoundException(nameof(Subscription), accountId);
        if (subscription.Version != expectedVersion)
            throw new ConflictException("The subscription was changed by someone else. Reload and retry.");
        if (subscription.Status == SubscriptionStatus.Canceled)
            return await GetAsync(accountId, cancellationToken);

        var before = new { subscriptionId = subscription.Id, status = subscription.Status.ToString(), planId = subscription.PlanId, version = subscription.Version };
        subscription.CancelImmediately(_operator.OperatorId, _clock.UtcNow);
        _audit.Record(new OperatorAuditRecord(
            "subscription.terminate", "subscription", subscription.Id, accountId, reason ?? string.Empty,
            before, new { subscriptionId = subscription.Id, status = subscription.Status.ToString(), version = subscription.Version }));
        await _commit.CommitAsync(cancellationToken);
        return await GetAsync(accountId, cancellationToken);
    }

    public async Task<OperatorAccountBillingDto> GrantOverrideAsync(
        Guid accountId, OperatorOverrideInput input, CancellationToken cancellationToken)
    {
        var code = input.FeatureCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!BillingCapabilityCode.All.Contains(code))
            throw new Notrelix.Application.Common.Exceptions.ValidationException($"'{code}' is not a capability the catalog knows. Known: {string.Join(", ", BillingCapabilityCode.All)}.");
        if (input.Limit < 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("The limit cannot be negative.");

        var now = _clock.UtcNow;
        if (input.ExpiresAt is { } expiry && expiry <= now)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("The expiry must be in the future.");

        await RequireAccountAsync(accountId, cancellationToken);
        var entitlement = Entitlement.Create(
            accountId, FeatureCode.Create(code), input.IsUnlimited ? 0 : input.Limit, EntitlementSource.Manual, now,
            EntitlementTargetScope.Account, null, input.ExpiresAt, input.IsUnlimited);
        _context.Entitlements.Add(entitlement);
        _audit.Record(new OperatorAuditRecord(
            "entitlement.grant", "entitlement", entitlement.Id, accountId, input.Reason ?? string.Empty, null,
            new { feature = code, limit = input.IsUnlimited ? (int?)null : input.Limit, unlimited = input.IsUnlimited, expiresAt = input.ExpiresAt }));
        await _commit.CommitAsync(cancellationToken);
        return await GetAsync(accountId, cancellationToken);
    }

    public async Task<OperatorAccountBillingDto> RevokeOverrideAsync(
        Guid entitlementId, string? reason, long expectedVersion, CancellationToken cancellationToken)
    {
        if (expectedVersion <= 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException("ExpectedVersion must be a positive value.");

        var entitlement = await _context.Entitlements.FirstOrDefaultAsync(e => e.Id == entitlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entitlement), entitlementId);
        if (entitlement.Source != EntitlementSource.Manual)
            throw new ConflictException("Only a manual override can be revoked here.");
        if (entitlement.Version != expectedVersion)
            throw new ConflictException("The override was changed by someone else. Reload and retry.");
        if (entitlement.Status == EntitlementStatus.Revoked)
            return await GetAsync(entitlement.AccountId, cancellationToken);

        var before = new { status = entitlement.Status.ToString(), version = entitlement.Version };
        entitlement.Revoke(_operator.OperatorId, _clock.UtcNow);
        _audit.Record(new OperatorAuditRecord(
            "entitlement.revoke", "entitlement", entitlement.Id, entitlement.AccountId, reason ?? string.Empty,
            before, new { status = entitlement.Status.ToString(), version = entitlement.Version }));
        await _commit.CommitAsync(cancellationToken);
        return await GetAsync(entitlement.AccountId, cancellationToken);
    }

    private async Task RequireAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await _accounts.GetAdmissionAsync(accountId, cancellationToken) is null)
            throw new NotFoundException("Account", accountId);
    }

    private async Task<Subscription?> LatestSubscriptionAsync(Guid accountId, bool track, CancellationToken cancellationToken)
    {
        var query = track ? _context.Subscriptions : _context.Subscriptions.AsNoTracking();
        return await query.Where(s => s.AccountId == accountId && s.WorkspaceId == null)
            .OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private static OperatorSubscriptionDto ToDto(Subscription s) =>
        new(s.Id, s.PlanId, s.Status.ToString(), s.Tier.ToString(), s.Version, s.CurrentPeriodEnd);

    private static OperatorOverrideDto ToDto(Entitlement e) =>
        new(e.Id, e.Feature.Code, e.IsUnlimited, e.Limit, e.Source.ToString(), e.Status.ToString(), e.Version, e.ExpiresAt, e.CreatedAt);
}
