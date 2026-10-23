using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Queries.GetWorkspaceAutomations;
using Notrelix.Application.Features.Automation.DTOs;
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

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// TAC-AI-FLOW-01 — the canonical-pipeline proof for rule creation: the
/// create command is write + idempotent, so the same Idempotency-Key with the
/// same request produces exactly one AutomationRule and replays the first
/// semantic result — the create never runs twice under one operation key.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CreateAutomationRulePipelineTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CreateAutomationRulePipelineTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SameIdempotencyKey_SameRequest_CreatesExactlyOneRule_AndReplays()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        using var provider = CreateProvider(accountId, ownerId);
        var command = new CreateAutomationRuleCommand(
            workspaceId,
            "Idempotent Rule",
            "ItemCreated",
            "Webhook",
            """{"webhookPath":"notrelix-idem"}""");

        Guid firstRuleId;
        using (var firstScope = provider.CreateScope())
        {
            BindKey(firstScope, "automation-create-key-1");
            var first = await firstScope.ServiceProvider.GetRequiredService<ISender>()
                .Send(command);
            first.Succeeded.Should().BeTrue("the first send creates the rule");
            firstRuleId = first.Data;
        }

        using (var secondScope = provider.CreateScope())
        {
            BindKey(secondScope, "automation-create-key-1");
            var second = await secondScope.ServiceProvider.GetRequiredService<ISender>()
                .Send(command);
            second.Succeeded.Should().BeTrue("a replay replays the first semantic result");
            second.Data.Should().Be(firstRuleId, "the replay carries the original rule id, not a new one");
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.AutomationRules.IgnoreQueryFilters()
                .CountAsync(r => r.WorkspaceId == workspaceId && r.Name == "Idempotent Rule"))
            .Should().Be(1, "the same operation key must never create a second rule");
    }

    [Fact]
    public async Task DifferentIdempotencyKeys_SameRequest_CreateTwoRules()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        using var provider = CreateProvider(accountId, ownerId);
        var command = new CreateAutomationRuleCommand(
            workspaceId,
            "Two Key Rule",
            "ItemCreated",
            "Webhook",
            """{"webhookPath":"notrelix-two-keys"}""");

        using (var firstScope = provider.CreateScope())
        {
            BindKey(firstScope, "automation-create-key-a");
            (await firstScope.ServiceProvider.GetRequiredService<ISender>().Send(command))
                .Succeeded.Should().BeTrue();
        }

        using (var secondScope = provider.CreateScope())
        {
            BindKey(secondScope, "automation-create-key-b");
            (await secondScope.ServiceProvider.GetRequiredService<ISender>().Send(command))
                .Succeeded.Should().BeTrue(
                    "two distinct operations are two legitimate rule creations, not duplicates");
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.AutomationRules.IgnoreQueryFilters()
                .CountAsync(r => r.WorkspaceId == workspaceId && r.Name == "Two Key Rule"))
            .Should().Be(2);
    }

    [Fact]
    public async Task Admin_CanCreateWebhookRule_ThroughBothRequiredPermissions_AndEnableIt()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        var admin = await SeedMemberAsync(accountId, workspaceId, WorkspaceRole.Admin);
        using var provider = CreateProvider(accountId, admin);

        Guid ruleId;
        using (var scope = provider.CreateScope())
        {
            BindKey(scope, "automation-admin-create");
            var created = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new CreateAutomationRuleCommand(workspaceId, "Admin hook", "ItemCreated", "Webhook", """{"webhookPath":"admin/hook"}"""));
            created.Succeeded.Should().BeTrue("ManageWorkspaceSettings and ManageIntegrations are both allowed for an Admin");
            ruleId = created.Data;
        }

        using (var scope = provider.CreateScope())
        {
            var enabled = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new SetAutomationRuleEnabledCommand(ruleId, true));
            enabled.Succeeded.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task MemberAndGuest_CannotCreateEnableListOrReadExecutions(WorkspaceRole role)
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        Guid ruleId;
        using (var ownerProvider = CreateProvider(accountId, ownerId))
        using (var scope = ownerProvider.CreateScope())
        {
            BindKey(scope, "automation-owner-create");
            ruleId = (await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new CreateAutomationRuleCommand(workspaceId, "Owner hook", "ItemCreated", "Webhook", """{"webhookPath":"owner/hook"}"""))).Data;
        }

        var user = await SeedMemberAsync(accountId, workspaceId, role);
        using var provider = CreateProvider(accountId, user);

        async Task DeniedAsync<T>(Func<ISender, Task<T>> send, string key)
        {
            using var scope = provider.CreateScope();
            BindKey(scope, key);
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await FluentActions.Awaiting(() => send(sender)).Should().ThrowAsync<Exception>();
        }

        await DeniedAsync(s => s.Send(new CreateAutomationRuleCommand(
            workspaceId, "Denied", "ItemCreated", "Webhook", """{"webhookPath":"denied/hook"}""")), "denied-create");
        await DeniedAsync(s => s.Send(new SetAutomationRuleEnabledCommand(ruleId, true)), "denied-enable");
        await DeniedAsync(s => s.Send(new GetWorkspaceAutomationsQuery(workspaceId)), "denied-list");
        await DeniedAsync(s => s.Send(new GetAutomationExecutionsQuery(ruleId)), "denied-executions");
    }

    [Fact]
    public async Task Owner_CanListRulesAndExecutions()
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        using var provider = CreateProvider(accountId, ownerId);
        Guid ruleId;
        using (var scope = provider.CreateScope())
        {
            BindKey(scope, "automation-owner-list");
            ruleId = (await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CreateAutomationRuleCommand(
                workspaceId, "Listed", "ItemCreated", "Webhook", """{"webhookPath":"listed/hook"}"""))).Data;
        }

        using (var scope = provider.CreateScope())
        {
            var rules = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetWorkspaceAutomationsQuery(workspaceId));
            rules.Succeeded.Should().BeTrue();
            rules.Data.Should().ContainSingle(r => r.Id == ruleId);
        }

        using (var scope = provider.CreateScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetAutomationExecutionsQuery(ruleId)))
                .Succeeded.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("../../rest/api")]
    [InlineData("a/%2e%2e/b")]
    [InlineData("hook?x=1")]
    [InlineData("hook#f")]
    [InlineData("a\\b")]
    public async Task Create_WebhookRuleWithUnsafePath_IsRejected_AndNothingIsPersisted(string path)
    {
        var (accountId, ownerId, workspaceId) = await SeedWorkspaceStackAsync();
        using var provider = CreateProvider(accountId, ownerId);
        using var scope = provider.CreateScope();
        BindKey(scope, "automation-traversal-" + Guid.NewGuid().ToString("N"));
        var configuration = System.Text.Json.JsonSerializer.Serialize(new { webhookPath = path });

        Func<Task> act = async () =>
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new CreateAutomationRuleCommand(workspaceId, "Traversal", "ItemCreated", "Webhook", configuration));
            if (result.Succeeded) return;
            throw new InvalidOperationException("rejected");
        };

        await act.Should().ThrowAsync<Exception>();
        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.AutomationRules.IgnoreQueryFilters().CountAsync(r => r.WorkspaceId == workspaceId && r.Name == "Traversal"))
            .Should().Be(0);
    }

    private async Task<Guid> SeedMemberAsync(Guid accountId, Guid workspaceId, WorkspaceRole role)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"auto-{Guid.NewGuid():N}@example.com", "Auto Member", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.AccountMembers.Add(AccountMember.Create(accountId, user.Id, AccountRole.Member, user.Id, now));
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(accountId, workspaceId, user.Id, role, user.Id, now));
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(accountId, workspaceId, user.Id, role, now, CancellationToken.None);
        await grant.SaveChangesAsync();
        return user.Id;
    }

    private static void BindKey(IServiceScope scope, string key) =>
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set(key, IdempotencyExecutionSource.Internal);

    private ServiceProvider CreateProvider(Guid accountId, Guid userId)
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

        var billingMock = new Mock<IBillingCapabilityFacts>();
        billingMock
            .Setup(b => b.GetCapabilityAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(IsAvailable: true, Limit: 100, Used: 0, Remaining: 100));

        var capacityMock = new Mock<IBillingCapacityActions>();
        capacityMock
            .Setup(c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsumeCapacityResult(AlreadyConsumed: false, Remaining: 99));

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
        services.AddSingleton(billingMock.Object);
        services.AddSingleton(capacityMock.Object);
        // AUT-020: Webhook rules never reach the Work port; the real MoveItem authority is proved in
        // AutomationMoveItemExecutorCompositionIntegrationTests.
        services.AddSingleton(new Mock<Notrelix.Application.Features.Automation.Ports.WorkManagement.IWorkActionPort>().Object);
        services.AddScoped<Notrelix.Application.Features.Automation.Rules.Services.AutomationExecutorAuthority>();

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
                sp.GetRequiredService<IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
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

        services.AddScoped<
            IRequestHandler<CreateAutomationRuleCommand, Result<Guid>>,
            CreateAutomationRuleCommandHandler>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(userId);
        services.AddSingleton(currentUser.Object);
        services.AddScoped<
            IRequestHandler<SetAutomationRuleEnabledCommand, Result>,
            SetAutomationRuleEnabledCommandHandler>();
        services.AddScoped<
            IRequestHandler<GetWorkspaceAutomationsQuery, Result<IReadOnlyList<AutomationRuleDto>>>,
            GetWorkspaceAutomationsQueryHandler>();
        services.AddScoped<
            IRequestHandler<GetAutomationExecutionsQuery, Result<IReadOnlyList<AutomationExecutionDto>>>,
            GetAutomationExecutionsQueryHandler>();

        return services.BuildServiceProvider();
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId)> SeedWorkspaceStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create($"auto-{Guid.NewGuid():N}@example.com", "Auto Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var account = Account.Create("Auto Account", $"auto-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "Auto WS", $"auto-ws-{Guid.NewGuid():N}", now);

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
}
