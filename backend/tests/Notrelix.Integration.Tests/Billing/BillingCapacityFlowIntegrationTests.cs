using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Capacity;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Billing;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Billing;

/// <summary>
/// Cross-boundary evidence for the M9 Billing capacity pack using the real
/// production graph (Application + Infrastructure + PostgreSQL):
/// <list type="bullet">
/// <item>TAC-XPK-BILLING-LAST-SLOT / TAC-BI-FLOW-03 — hard-quota production race</item>
/// <item>TAC-BI-008B / TAC-BI-FLOW-03 retry — usage write idempotency</item>
/// <item>BOUND-TX-003 / TAC-BI-FLOW-03 compensation — failure atomicity</item>
/// <item>TAC-BI-FLOW-04 / TAC-BI-008C — production lifecycle feeds the ledger</item>
/// <item>TAC-BI-FLOW-02 — release at full capacity preserves the finite ceiling</item>
/// <item>TAC-BI-FLOW-03 — a disabled rule still occupies its capacity slot</item>
/// <item>TAC-BI-006 retry — same idempotency key never double-uses capacity; usage is workspace-isolated</item>
/// </list>
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BillingCapacityFlowIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BillingCapacityFlowIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "TAC-XPK-BILLING-LAST-SLOT - two concurrent creates, one remaining slot")]
    public async Task TwoConcurrentCreateRuleCommands_OneRemainingSlot_ExactlyOneWins_AndOneSloteConsumed()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedWorkspaceFeatureUsageAsync(accountId, workspaceId, currentUsage: 0, hardLimit: 1);

        using var provider = CreateProvider(accountId, ownerId, new BillingCapabilityFact(
            IsAvailable: true, Limit: 1, Used: 0, Remaining: 1));

        var first = CreateRuleCommand(workspaceId, "Race Rule A", "race-a");
        var second = CreateRuleCommand(workspaceId, "Race Rule B", "race-b");

        using var firstScope = provider.CreateScope();
        BindKey(firstScope, "automation-race-key-a");
        using var secondScope = provider.CreateScope();
        BindKey(secondScope, "automation-race-key-b");

        var outcomes = await Task.WhenAll(
            SendOnceAsync(firstScope, first),
            SendOnceAsync(secondScope, second));

        outcomes.Count(o => o.Outcome == CreateRuleOutcome.Succeeded)
            .Should().Be(1, "exactly one of the concurrent creates may win the single remaining slot");
        outcomes.Where(o => o.Outcome != CreateRuleOutcome.Succeeded)
            .Should().OnlyContain(o => o.Outcome == CreateRuleOutcome.VersionConflict
                || o.Outcome == CreateRuleOutcome.UniqueConflict
                || o.Outcome == CreateRuleOutcome.CapacityExceeded
                || o.Outcome == CreateRuleOutcome.GateRejected,
                "the losing request must receive a canonical capacity/concurrency failure");
        var winner = outcomes.Single(o => o.Outcome == CreateRuleOutcome.Succeeded);

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.AutomationRules.IgnoreQueryFilters().CountAsync(r => r.WorkspaceId == workspaceId))
            .Should().Be(1, "a loser that reached SaveChanges must roll back the rule insert atomically");
        (await verify.AutomationRules.IgnoreQueryFilters()
                .AnyAsync(r => r.WorkspaceId == workspaceId && r.Name == winner.Name))
            .Should().BeTrue("the surviving rule is exactly the request that reported success");

        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(1m, "exactly one capacity slot is consumed");
        usage.HardLimit.Should().Be(1m);
        usage.Version.Should().Be(2, "the authoritative usage row advanced exactly once via the version token");

        var ledger = await verify.FeatureUsageLedger
            .Where(l => l.AccountId == accountId
                && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule)
            .ToListAsync();
        ledger.Should().HaveCount(1, "the one consumed slot records exactly one ledger effect");
        ledger.Single().Delta.Should().Be(1m);
        ledger.Single().LogicalOperationId.Should().Be(winner.RuleId);
        ledger.Single().ReferenceResource.Should().Be(winner.RuleId.ToString());
    }

    [Fact(DisplayName = "TAC-BI-008B - usage write idempotency on the real DB")]
    public async Task SameLogicalOperation_CapacityReplaysWithoutDoubleEffect_AndConflictingPayloadFails()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 5);

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, ownerId);
        await using var context = _db.CreateContext(tenant);
        await using var tx = await context.Database.BeginTransactionAsync();
        var capacity = new BillingCapacityActions(
            context,
            new BillingCapabilityFactsProvider(context, clock.Object));

        var op = new BillingCapacityOperationIdentity(
            accountId, workspaceId, BillingCapabilityCode.AutomationRule,
            Amount: 1, LogicalOperationId: Guid.NewGuid(), SourceResource: "rule-a",
            ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);

        var first = await capacity.ConsumeAsync(new ConsumeCapacityRequest(op), CancellationToken.None);
        first.AlreadyConsumed.Should().BeFalse("first execution reserves the slot");
        first.Remaining.Should().Be(4m);
        await context.SaveChangesAsync();

        var replay = await capacity.ConsumeAsync(new ConsumeCapacityRequest(op), CancellationToken.None);
        replay.AlreadyConsumed.Should().BeTrue("the same logical operation replays its first result");
        replay.Remaining.Should().Be(4m);
        await context.SaveChangesAsync();

        var conflicting = op with { SourceResource = "rule-b" };
        var conflict = () => capacity.ConsumeAsync(new ConsumeCapacityRequest(conflicting), CancellationToken.None);
        await conflict.Should().ThrowAsync<CapacityOperationConflictException>(
            "reusing an executed logical operation id with a different payload is a deterministic conflict");
        await context.SaveChangesAsync();

        var releaseOp = new BillingCapacityOperationIdentity(
            accountId, workspaceId, BillingCapabilityCode.AutomationRule,
            Amount: 1, LogicalOperationId: Guid.NewGuid(), SourceResource: "rule-a",
            ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);
        var release = await capacity.ReleaseAsync(new ReleaseCapacityRequest(releaseOp), CancellationToken.None);
        release.AlreadyReleased.Should().BeFalse("release returns the slot to the pool");
        release.Remaining.Should().Be(5m);
        await context.SaveChangesAsync();
        var releaseReplay = await capacity.ReleaseAsync(new ReleaseCapacityRequest(releaseOp), CancellationToken.None);
        releaseReplay.AlreadyReleased.Should().BeTrue("a repeated release is deduplicated");
        releaseReplay.Remaining.Should().Be(5m);
        await tx.CommitAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var ledger = await verify.FeatureUsageLedger
            .Where(l => l.AccountId == accountId
                && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule)
            .ToListAsync();
        ledger.Should().HaveCount(2, "one +1 consume and one -1 release, never a retry duplicate");
        ledger.Count(l => l.Delta == 1).Should().Be(1);
        ledger.Count(l => l.Delta == -1).Should().Be(1);
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(0m, "the released capacity is restored");
        usage.HardLimit.Should().Be(5m);
    }

    [Fact(DisplayName = "TAC-BI-FLOW-02 - direct last-slot race, two transactions, exactly one wins")]
    public async Task DirectAction_LastSlotRace_TwoTransactions_ExactlyOneWins()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedWorkspaceFeatureUsageAsync(accountId, workspaceId, currentUsage: 1, hardLimit: 2);
        await SeedEntitlementAsync(accountId, limit: 2);

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        async Task<bool> TryConsumeAsync(string sourceResource)
        {
            var tenant = new FakeCurrentTenantContext();
            tenant.SetWorkspace(accountId, workspaceId, ownerId);
            await using var ctx = _db.CreateContext(tenant);
            var capacity = new BillingCapacityActions(ctx, new BillingCapabilityFactsProvider(ctx, clock.Object));
            var op = new BillingCapacityOperationIdentity(
                accountId, workspaceId, BillingCapabilityCode.AutomationRule,
                Amount: 1, LogicalOperationId: Guid.CreateVersion7(), SourceResource: sourceResource,
                ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);
            await using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                var result = await capacity.ConsumeAsync(new ConsumeCapacityRequest(op), CancellationToken.None);
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
                return !result.AlreadyConsumed;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Lost the optimistic-concurrency race at SaveChanges.
                return false;
            }
            catch (BusinessRuleException)
            {
                // Lost the last slot before SaveChanges: the usage load observed
                // the winner's committed current usage and the Domain limit guard
                // denied the new consumption. Both mechanisms are legitimate
                // single-winner outcomes of the same race.
                return false;
            }
        }

        var outcomes = await Task.WhenAll(TryConsumeAsync("slot-a"), TryConsumeAsync("slot-b"));

        outcomes.Count(o => o).Should().Be(1, "exactly one transaction wins the single remaining slot");
        await using var verify = _db.CreateContext(SystemTenant());
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(2m, "exactly one capacity slot is consumed into the reserved row");
        usage.Version.Should().Be(2, "the version token advanced exactly once for the single winning consume");
        var ledger = await verify.FeatureUsageLedger
            .Where(l => l.AccountId == accountId
                && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule)
            .ToListAsync();
        ledger.Should().HaveCount(1, "the one consumed slot records exactly one ledger effect");
        ledger.Single().Delta.Should().Be(1m);
    }

    [Fact(DisplayName = "TAC-BI-FLOW-02 - first-use bootstrap race, two transactions, both served")]
    public async Task FirstUse_ConcurrentConsumes_OnFreshScope_BothSucceed_NoFalseUniqueConflict()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 2);
        // Intentionally NO WorkspaceFeatureUsage row — both consumers race the atomic first-use seed.

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        async Task<bool> TryConsumeAsync(string sourceResource)
        {
            var tenant = new FakeCurrentTenantContext();
            tenant.SetWorkspace(accountId, workspaceId, ownerId);
            await using var ctx = _db.CreateContext(tenant);
            var capacity = new BillingCapacityActions(ctx, new BillingCapabilityFactsProvider(ctx, clock.Object));
            var op = new BillingCapacityOperationIdentity(
                accountId, workspaceId, BillingCapabilityCode.AutomationRule,
                Amount: 1, LogicalOperationId: Guid.CreateVersion7(), SourceResource: sourceResource,
                ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);
            await using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                var result = await capacity.ConsumeAsync(new ConsumeCapacityRequest(op), CancellationToken.None);
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
                return !result.AlreadyConsumed;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(TryConsumeAsync("boot-a"), TryConsumeAsync("boot-b"));
        outcomes.Should().OnlyContain(o => o,
            "the atomic first-use seed must serve both consumers up to the limit, never a false unique-scope conflict");

        await using var verify = _db.CreateContext(SystemTenant());
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(2m, "both consumers succeeded against the finite limit of 2");
        usage.HardLimit.Should().Be(2m);
        var ledger = await verify.FeatureUsageLedger
            .Where(l => l.AccountId == accountId
                && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule)
            .ToListAsync();
        ledger.Should().HaveCount(2, "two distinct logical operations record two +1 effects");
        ledger.Should().OnlyContain(l => l.Delta == 1m);
    }

    [Fact(DisplayName = "TAC-BI-FLOW-02 - first-use seeds CurrentUsage from the ledger sum")]
    public async Task FirstUse_WithPriorLedgerHistory_SeedsCurrentUsageFromLedgerSum()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 5);
        await SeedLedgerDeltaAsync(accountId, workspaceId, 2, ownerId); // committed history, no WFU row yet

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, ownerId);
        await using var context = _db.CreateContext(tenant);
        await using var tx = await context.Database.BeginTransactionAsync();
        var capacity = new BillingCapacityActions(
            context,
            new BillingCapabilityFactsProvider(context, clock.Object));
        var op = new BillingCapacityOperationIdentity(
            accountId, workspaceId, BillingCapabilityCode.AutomationRule,
            Amount: 1, LogicalOperationId: Guid.CreateVersion7(), SourceResource: "materialized-history",
            ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);

        var result = await capacity.ConsumeAsync(new ConsumeCapacityRequest(op), CancellationToken.None);

        result.Remaining.Should().Be(2m, "two historical units are already consumed from the five-unit grant");
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(3m, "the seed materialized the ledger sum (2) and the new consume added 1");
        usage.HardLimit.Should().Be(5m);
        var ledger = await verify.FeatureUsageLedger
            .Where(l => l.AccountId == accountId
                && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule)
            .ToListAsync();
        ledger.Should().HaveCount(2);
        ledger.Should().ContainSingle(l => l.Delta == 2m);
        ledger.Should().ContainSingle(l => l.Delta == 1m);
    }

    [Fact(DisplayName = "BOUND-TX-003 - capacity consume rolls back with downstream failure")]
    public async Task ConsumeThenDownstreamFailure_EverythingRollsBack_NoCapacityOrLedgerEffect()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 5);

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, ownerId);
        await using var context = _db.CreateContext(tenant);
        var rls = new RlsSessionContext(
            context,
            Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }),
            tenant);
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var session = new EfRequestDataSession(
            context,
            rls,
            loggerFactory.CreateLogger<EfRequestDataSession>());

        var op = new BillingCapacityOperationIdentity(
            accountId, workspaceId, BillingCapabilityCode.AutomationRule,
            Amount: 1, LogicalOperationId: Guid.NewGuid(), SourceResource: "rule-compensated",
            ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);

        Func<Task<bool>> attempt = () => session.ExecuteAsync<bool>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: true,
                ApplyResourceScope: false),
            async ct =>
            {
                var capacity = new BillingCapacityActions(
                    context,
                    new BillingCapabilityFactsProvider(context, clock.Object));
                await capacity.ConsumeAsync(new ConsumeCapacityRequest(op), ct);
                throw new ConflictException("simulated AutomationRule persistence failure");
            },
            CancellationToken.None);

        await attempt.Should().ThrowAsync<ConflictException>(
            "the downstream failure surfaces to the caller");
        await using (var verify = _db.CreateContext(SystemTenant()))
        {
            (await verify.WorkspaceFeatureUsages
                    .CountAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId))
                .Should().Be(0, "a rolled-back session leaves no usage row");
            (await verify.FeatureUsageLedger
                    .CountAsync(l => l.AccountId == accountId && l.WorkspaceId == workspaceId))
                .Should().Be(0, "a rolled-back session leaves no ledger effect");
        }
    }

    [Fact(DisplayName = "TAC-BI-008C - real capability reflects production AutomationRule lifecycle")]
    public async Task RealCapabilityLifecycle_CreateRules_ConsumesSlots_ThenGateRejectsAtLimit()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 3);

        using var provider = CreateProvider(accountId, ownerId, gateFact: null);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        (await ReadRemainingAsync(accountId, workspaceId, clock.Object))
            .Should().Be(3, "capability before create reflects the granted limit");

        for (var i = 1; i <= 3; i++)
        {
            var result = await SendCreateAsync(provider, $"automation-lifecycle-key-{i}", CreateRuleCommand(workspaceId, $"Lifecycle Rule {i}", $"lifecycle-{i}"));
            result.Succeeded.Should().BeTrue($"rule {i} fits under the plan limit");
            (await ReadRemainingAsync(accountId, workspaceId, clock.Object))
                .Should().Be(3 - i, "each persisted rule consumes exactly one ledgered slot");
        }

        var fourth = await SendCreateAsync(provider, "automation-lifecycle-key-4", CreateRuleCommand(workspaceId, "Lifecycle Rule 4", "lifecycle-4"));
        fourth.Succeeded.Should().BeFalse("the fourth rule exceeds the plan limit and the gate rejects");

        (await ReadRemainingAsync(accountId, workspaceId, clock.Object)).Should().Be(0);

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.AutomationRules.IgnoreQueryFilters().CountAsync(r => r.WorkspaceId == workspaceId))
            .Should().Be(3, "no rule is created past the limit");
        (await verify.FeatureUsageLedger.CountAsync(l =>
                l.AccountId == accountId && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule && l.Delta == 1))
            .Should().Be(3, "the production lifecycle writes the ledger; capability follows it");
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(3m);
    }

    [Fact(DisplayName = "TAC-BI-FLOW-02 - release at full capacity keeps the finite ceiling")]
    public async Task ReleaseAtFullCapacity_PreservesFiniteCeiling_AndRestoresHeadroom()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedWorkspaceFeatureUsageAsync(accountId, workspaceId, currentUsage: 2, hardLimit: 2);
        await SeedEntitlementAsync(accountId, limit: 2);

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, ownerId);
        await using var context = _db.CreateContext(tenant);
        await using var tx = await context.Database.BeginTransactionAsync();
        var capacity = new BillingCapacityActions(
            context,
            new BillingCapabilityFactsProvider(context, clock.Object));
        var op = new BillingCapacityOperationIdentity(
            accountId, workspaceId, BillingCapabilityCode.AutomationRule,
            Amount: 1, LogicalOperationId: Guid.CreateVersion7(), SourceResource: "release-at-full",
            ActorUserId: ownerId, OccurredAt: clock.Object.UtcNow);

        var release = await capacity.ReleaseAsync(new ReleaseCapacityRequest(op), CancellationToken.None);

        release.AlreadyReleased.Should().BeFalse("release at full capacity restores headroom, never zeroes the grant");
        release.Remaining.Should().Be(1m, "releasing one of the two granted units leaves one headroom");
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(1m);
        usage.HardLimit.Should().Be(2m, "the finite grant remains the ceiling after the release");
        (await verify.FeatureUsageLedger.CountAsync(l =>
                l.AccountId == accountId && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule && l.Delta == -1))
            .Should().Be(1, "the release records exactly one -1 ledger effect");
    }

    [Fact(DisplayName = "TAC-BI-FLOW-03 - a disabled rule still occupies its capacity slot")]
    public async Task DisablingARule_DoesNotReleaseCapacity()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 3);

        using var provider = CreateProvider(accountId, ownerId, gateFact: null);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var created = await SendCreateAsync(provider, "disable-key-1", CreateRuleCommand(workspaceId, "Disabled Rule", "disable-me"));
        created.Succeeded.Should().BeTrue();
        (await ReadRemainingAsync(accountId, workspaceId, clock.Object))
            .Should().Be(2, "one slot is consumed by the created rule");

        using (var scope = provider.CreateScope())
        {
            BindKey(scope, "disable-rule-attempt");
            var disabled = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new SetAutomationRuleEnabledCommand(created.Data, false));
            disabled.Succeeded.Should().BeTrue();
        }

        (await ReadRemainingAsync(accountId, workspaceId, clock.Object))
            .Should().Be(2, "disabling a rule does not return its slot to the pool");

        await using var verify = _db.CreateContext(SystemTenant());
        var rule = await verify.AutomationRules.IgnoreQueryFilters()
            .SingleAsync(r => r.Id == created.Data);
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        var usage = await verify.WorkspaceFeatureUsages
            .SingleAsync(w => w.AccountId == accountId && w.WorkspaceId == workspaceId);
        usage.CurrentUsage.Should().Be(1m, "the disabled rule still occupies one slot");
        (await verify.FeatureUsageLedger.CountAsync(l =>
                l.AccountId == accountId && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule && l.Delta == 1))
            .Should().Be(1, "exactly the one create consume is ledgered");
        (await verify.FeatureUsageLedger.CountAsync(l =>
                l.AccountId == accountId && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule && l.Delta == -1))
            .Should().Be(0, "a mere disable adds no release effect");
    }

    [Fact(DisplayName = "TAC-BI-006 - CreateAutomationRule retry with the same idempotency key never double-uses capacity")]
    public async Task CreateRule_RetrySameIdempotencyKey_ConsumesOneSlotOnly()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        await SeedEntitlementAsync(accountId, limit: 3);

        using var provider = CreateProvider(accountId, ownerId, gateFact: null);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var command = CreateRuleCommand(workspaceId, "Retried Rule", "retry-me");
        var first = await SendCreateAsync(provider, "retry-same-key", command);
        first.Succeeded.Should().BeTrue();
        var replay = await SendCreateAsync(provider, "retry-same-key", command);
        replay.Succeeded.Should().BeTrue("the same operation key replays the first semantic result");

        (await ReadRemainingAsync(accountId, workspaceId, clock.Object))
            .Should().Be(2, "the replayed request must not consume a second slot");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.AutomationRules.IgnoreQueryFilters().CountAsync(r => r.WorkspaceId == workspaceId))
            .Should().Be(1, "the retry must not create a second rule");
        (await verify.FeatureUsageLedger.CountAsync(l =>
                l.AccountId == accountId && l.WorkspaceId == workspaceId
                && l.FeatureCode == BillingCapabilityCode.AutomationRule && l.Delta == 1))
            .Should().Be(1, "the retry must not record a second +1 ledger effect");
    }

    [Fact(DisplayName = "TAC-BI-006 - CreateAutomationRule usage is isolated per workspace")]
    public async Task CreateRuleInOneWorkspace_DoesNotConsumeCapacityOfAnother()
    {
        var (accountId, ownerId, workspaceIdA) = await SeedWorkspaceStackAsync();
        var workspaceIdB = await SeedAdditionalWorkspaceAsync(accountId, ownerId, "Billing WS B", $"billing-ws-b-{Guid.NewGuid():N}");
        await SeedEntitlementAsync(accountId, limit: 3);

        using var provider = CreateProvider(accountId, ownerId, gateFact: null);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var created = await SendCreateAsync(provider, "iso-key-a", CreateRuleCommand(workspaceIdA, "Isolation Rule", "iso-a"));
        created.Succeeded.Should().BeTrue();

        (await ReadRemainingAsync(accountId, workspaceIdA, clock.Object))
            .Should().Be(2, "workspace A consumed one slot");
        (await ReadRemainingAsync(accountId, workspaceIdB, clock.Object))
            .Should().Be(3, "workspace B is untouched by workspace A's usage under the same account grant");
    }

    private static CreateAutomationRuleCommand CreateRuleCommand(Guid workspaceId, string name, string path) =>
        new(workspaceId, name, "ItemCreated", "Webhook", $$"""{"webhookPath":"{{path}}"}""");

    private static async Task<Result<Guid>> SendCreateAsync(
        ServiceProvider provider,
        string key,
        CreateAutomationRuleCommand command)
    {
        using var scope = provider.CreateScope();
        BindKey(scope, key);
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    private static void BindKey(IServiceScope scope, string key) =>
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set(key, IdempotencyExecutionSource.Internal);

    private static async Task<CreateRuleAttempt> SendOnceAsync(
        IServiceScope scope,
        CreateAutomationRuleCommand command)
    {
        try
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
            return result.Succeeded
                ? new CreateRuleAttempt(CreateRuleOutcome.Succeeded, command.Name, result.Data)
                : new CreateRuleAttempt(CreateRuleOutcome.GateRejected, command.Name, null);
        }
        catch (PreconditionFailedException)
        {
            return new CreateRuleAttempt(CreateRuleOutcome.VersionConflict, command.Name, null);
        }
        catch (ConflictException)
        {
            return new CreateRuleAttempt(CreateRuleOutcome.UniqueConflict, command.Name, null);
        }
        catch (BusinessRuleException bre) when (bre.RuleCode == BillingRuleCodes.Billing_Usage_FeatureLimitExceeded)
        {
            return new CreateRuleAttempt(CreateRuleOutcome.CapacityExceeded, command.Name, null);
        }
    }

    private ServiceProvider CreateProvider(Guid accountId, Guid userId, BillingCapabilityFact? gateFact)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(c => c.UserId).Returns(userId);
        currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(CreateAutomationRuleCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<IPageAuthorizationFacts, PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IPageAuthorizationFacts>()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());

        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddScoped<IBillingDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        if (gateFact is not null)
        {
            var billingMock = new Mock<IBillingCapabilityFacts>();
            billingMock
                .Setup(b => b.GetCapabilityAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(gateFact);
            services.AddSingleton<IBillingCapabilityFacts>(billingMock.Object);
        }
        else
        {
            services.AddScoped<IBillingCapabilityFacts>(sp =>
                new BillingCapabilityFactsProvider(
                    sp.GetRequiredService<IBillingDbContext>(),
                    sp.GetRequiredService<IDateTimeProvider>()));
        }

        services.AddScoped<IBillingCapacityActions>(sp =>
            new BillingCapacityActions(
                sp.GetRequiredService<IBillingDbContext>(),
                sp.GetRequiredService<IBillingCapabilityFacts>()));

        services.AddScoped<
            IRequestHandler<CreateAutomationRuleCommand, Result<Guid>>,
            CreateAutomationRuleCommandHandler>();

        services.AddScoped<
            IRequestHandler<SetAutomationRuleEnabledCommand, Result>,
            SetAutomationRuleEnabledCommandHandler>();

        return services.BuildServiceProvider();
    }

    private async Task<int?> ReadRemainingAsync(Guid accountId, Guid workspaceId, IDateTimeProvider clock)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, null);
        await using var context = _db.CreateContext(tenant);
        var provider = new BillingCapabilityFactsProvider(context, clock);
        var fact = await provider.GetCapabilityAsync(
            accountId, workspaceId, BillingCapabilityCode.AutomationRule, requestedAmount: 1, CancellationToken.None);
        return fact?.Remaining;
    }

    [Fact(DisplayName = "TAC-BI-FLOW-02 - the global ledger index rejects a duplicated logical operation id across scopes")]
    public async Task GlobalLogicalOperationIndex_RejectsDuplicateAcrossScopes()
    {
        var (accountA, ownerA, workspaceA) = await SeedWorkspaceStackAsync();
        var (accountB, _, workspaceB) = await SeedWorkspaceStackAsync();
        var logicalOperationId = Guid.CreateVersion7();

        await SeedLedgerDeltaAsync(accountA, workspaceA, 1, ownerA, logicalOperationId);

        await using var context = _db.CreateContext(SystemTenant());
        context.FeatureUsageLedger.Add(FeatureUsageLedger.Create(
            accountB,
            workspaceB,
            BillingCapabilityCode.AutomationRule,
            1m,
            ownerA,
            referenceResource: "cross-scope-duplicate",
            note: null,
            DateTimeOffset.UtcNow,
            logicalOperationId));
        var insert = () => context.SaveChangesAsync();
        await insert.Should().ThrowAsync<DbUpdateException>(
            "the global partial unique index on logical_operation_id must reject the duplicate across scopes at the database");
    }

    private async Task SeedWorkspaceFeatureUsageAsync(
        Guid accountId, Guid workspaceId, decimal currentUsage, decimal hardLimit)
    {
        var now = DateTimeOffset.UtcNow;
        await using var seed = _db.CreateContext(SystemTenant());
        seed.WorkspaceFeatureUsages.Add(WorkspaceFeatureUsage.Create(
            accountId,
            workspaceId,
            FeatureCode.Create(BillingCapabilityCode.AutomationRule),
            currentUsage,
            hardLimit,
            softLimit: hardLimit,
            now));
        await seed.SaveChangesAsync();
    }

    private async Task<Guid> SeedAdditionalWorkspaceAsync(
        Guid accountId, Guid ownerId, string name, string slug)
    {
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, name, slug, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now));
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspace.Id, ownerId, WorkspaceRole.Owner, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return workspace.Id;
    }

    private async Task SeedEntitlementAsync(Guid accountId, int limit)
    {
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Entitlements.Add(Entitlement.Create(
            accountId,
            FeatureCode.Create(BillingCapabilityCode.AutomationRule),
            limit,
            EntitlementSource.Subscription,
            DateTimeOffset.UtcNow));
        await seed.SaveChangesAsync();
    }

    private async Task SeedLedgerDeltaAsync(
        Guid accountId,
        Guid workspaceId,
        decimal delta,
        Guid actorUserId,
        Guid? logicalOperationId = null)
    {
        await using var seed = _db.CreateContext(SystemTenant());
        seed.FeatureUsageLedger.Add(FeatureUsageLedger.Create(
            accountId,
            workspaceId,
            BillingCapabilityCode.AutomationRule,
            delta,
            actorUserId,
            referenceResource: "historical-usage",
            note: null,
            DateTimeOffset.UtcNow,
            logicalOperationId ?? Guid.CreateVersion7()));
        await seed.SaveChangesAsync();
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId)> SeedWorkspaceStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"billing-{Guid.NewGuid():N}@example.com", "Billing Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var account = Account.Create("Billing Account", $"billing-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Billing WS", $"billing-ws-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(owner);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return (account.Id, owner.Id, workspace.Id);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private enum CreateRuleOutcome
    {
        Succeeded,
        GateRejected,
        VersionConflict,
        UniqueConflict,
        CapacityExceeded,
    }

    private sealed record CreateRuleAttempt(CreateRuleOutcome Outcome, string Name, Guid? RuleId);
}