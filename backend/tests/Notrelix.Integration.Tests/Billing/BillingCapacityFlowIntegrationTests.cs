using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
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