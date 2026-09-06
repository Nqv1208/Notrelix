using Notrelix.Application.Common.Diagnostics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using ExecutionContextClass = Notrelix.Application.Common.Context.ExecutionContext;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// IA-TST-EV-DB — fail-closed optimistic-concurrency acceptance on real
/// PostgreSQL (freeze file 02 §7/§9): binding misconfigurations are server-side
/// failures, stale versions are client precondition failures that roll back the
/// whole authoritative transaction, and concurrent writers with the same declared
/// version produce exactly one winner.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ExpectedVersionConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public ExpectedVersionConcurrencyIntegrationTests(PostgresTestContainer db)
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
    public async Task MatchingVersion_CommitsAndAdvancesVersion()
    {
        var (workspace, ownerId) = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant());
        var tracked = await context.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == workspace.Id);
        var originalVersion = tracked.Version;

        var session = CreateSession(context);
        var response = await session.ExecuteAsync(
            Options(Constraint<UpdateWorkspaceProfileCommand>(workspace.Id, originalVersion)),
            _ =>
            {
                tracked.Rename("Matched writer", ownerId, DateTimeOffset.UtcNow);
                return Task.FromResult(true);
            },
            CancellationToken.None);

        response.Should().BeTrue();
        tracked.Version.Should().Be(originalVersion + 1);

        var persisted = await PersistedWorkspace(workspace.Id);
        persisted.Name.Should().Be("Matched writer");
        persisted.Version.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task StaleVersion_PreconditionFails_RollsBackBusinessMutation()
    {
        var (workspace, ownerId) = await SeedWorkspaceAsync();

        await using var firstContext = _db.CreateContext(SystemTenant());
        await using var secondContext = _db.CreateContext(SystemTenant());
        var first = await firstContext.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == workspace.Id);
        var second = await secondContext.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == workspace.Id);
        var winningVersion = first.Version;

        var winner = CreateSession(firstContext);
        await winner.ExecuteAsync(
            Options(Constraint<UpdateWorkspaceProfileCommand>(workspace.Id, winningVersion)),
            _ =>
            {
                first.Rename("First writer", ownerId, DateTimeOffset.UtcNow.AddSeconds(1));
                return Task.FromResult(true);
            },
            CancellationToken.None);

        var loser = CreateSession(secondContext);
        var staleWrite = () => loser.ExecuteAsync(
            Options(Constraint<UpdateWorkspaceProfileCommand>(workspace.Id, winningVersion)),
            _ =>
            {
                second.Rename("Second writer", ownerId, DateTimeOffset.UtcNow.AddSeconds(2));
                return Task.FromResult(true);
            },
            CancellationToken.None);

        (await staleWrite.Should().ThrowAsync<PreconditionFailedException>())
            .Which.ErrorCode.Should().Be("common.precondition-failed");

        var persisted = await PersistedWorkspace(workspace.Id);
        persisted.Name.Should().Be("First writer", "the stale write must roll back entirely");
        persisted.Version.Should().Be(winningVersion + 1);
    }

    [Fact]
    public async Task ConcurrentWriters_WithSameVersion_ExactlyOneCommits()
    {
        var (workspace, ownerId) = await SeedWorkspaceAsync();
        var sharedDeclaredVersion = workspace.Version;

        var tasks = Enumerable.Range(0, 4).Select(async index =>
        {
            await using var context = _db.CreateContext(SystemTenant());
            var tracked = await context.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == workspace.Id);
            var session = CreateSession(context);
            try
            {
                await session.ExecuteAsync(
                    Options(Constraint<UpdateWorkspaceProfileCommand>(workspace.Id, sharedDeclaredVersion)),
                    _ =>
                    {
                        tracked.Rename($"Writer {index}", ownerId, DateTimeOffset.UtcNow);
                        return Task.FromResult(true);
                    },
                    CancellationToken.None);
                return true;
            }
            catch (PreconditionFailedException)
            {
                return false;
            }
        }).ToArray();

        var outcomes = await Task.WhenAll(tasks);

        outcomes.Count(success => success).Should().Be(1,
            "all writers declared the same original version; exactly one may win");
        var persisted = await PersistedWorkspace(workspace.Id);
        persisted.Version.Should().Be(sharedDeclaredVersion + 1);
        persisted.Name.Should().StartWith("Writer ");
    }

    [Fact]
    public async Task RequestMissingFromTargetMap_FailsClosedAsMisconfiguration()
    {
        var (workspace, _) = await SeedWorkspaceAsync();
        await using var context = _db.CreateContext(SystemTenant());

        var constraint = new ExpectedVersionConstraint(
            typeof(UnmappedProbeRequest),
            ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), workspace.Id),
            1);

        var act = () => CreateSession(context).ExecuteAsync(
            Options(constraint),
            _ => Task.FromResult(true),
            CancellationToken.None);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain(nameof(UnmappedProbeRequest));
    }

    [Fact]
    public async Task DeclaredKindMismatch_FailsClosedAsMisconfiguration()
    {
        var (workspace, _) = await SeedWorkspaceAsync();
        await using var context = _db.CreateContext(SystemTenant());
        var tracked = await context.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == workspace.Id);

        // The mapped request type declares workspaces.workspace; feed it a board kind.
        var constraint = new ExpectedVersionConstraint(
            typeof(UpdateWorkspaceProfileCommand),
            ResourceRef.Create(ResourceKind.Create("work-management.board"), workspace.Id),
            tracked.Version);

        var act = () => CreateSession(context).ExecuteAsync(
            Options(constraint),
            _ => Task.FromResult(true),
            CancellationToken.None);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain("work-management.board");
    }

    [Fact]
    public async Task MappedTargetNotTracked_FailsClosedInsteadOfSilentSkip()
    {
        var (workspace, _) = await SeedWorkspaceAsync();
        await using var freshContext = _db.CreateContext(SystemTenant());
        // No tracking: the aggregate exists in the database but was never loaded.

        var constraint = Constraint<UpdateWorkspaceProfileCommand>(workspace.Id, workspace.Version);
        var act = () => CreateSession(freshContext).ExecuteAsync(
            Options(constraint),
            _ => Task.FromResult(true),
            CancellationToken.None);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain("no matching aggregate");
    }

    [Fact]
    public async Task SameGuidTrackedUnderDifferentAggregateType_FailsClosed()
    {
        var (workspace, _) = await SeedWorkspaceAsync();
        await using var context = _db.CreateContext(SystemTenant());

        // Track a Form whose identity collides with the workspace id while the
        // map binds this request type to the Workspace aggregate.
        var form = Notrelix.Domain.WorkManagement.Forms.Form.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Colliding form",
            $"colliding-{Guid.NewGuid():N}", Guid.NewGuid(), DateTimeOffset.UtcNow);
        SetEntityId(form, workspace.Id);
        context.Forms.Add(form);
        context.ChangeTracker.DetectChanges();

        var constraint = Constraint<UpdateWorkspaceProfileCommand>(workspace.Id, 1);
        var act = () => CreateSession(context).ExecuteAsync(
            Options(constraint),
            _ => Task.FromResult(true),
            CancellationToken.None);

        (await act.Should().ThrowAsync<SecurityMisconfigurationException>())
            .Which.Message.Should().Contain(typeof(Domain.Workspaces.Workspaces.Workspace).Name);
    }

    [Fact]
    public async Task PipelineStaleVersion_NoBusinessCommit_AndNoOutboxIntent()
    {
        var seeded = await SeedFullGraphAsync();
        using var provider = BuildPipelineProvider(seeded.AccountId, seeded.OwnerId);
        var current = await PersistedWorkspace(seeded.WorkspaceId);

        using (var scope = provider.CreateScope())
        {
            var ok = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new UpdateWorkspaceProfileCommand(
                    seeded.WorkspaceId, "Renamed by pipeline", null, current.Version));
            ok.Succeeded.Should().BeTrue();
        }

        var afterFirst = await PersistedWorkspace(seeded.WorkspaceId);
        afterFirst.Name.Should().Be("Renamed by pipeline");

        using (var scope = provider.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var stale = () => sender.Send(new UpdateWorkspaceProfileCommand(
                seeded.WorkspaceId, "Lost race", null, current.Version));

            (await stale.Should().ThrowAsync<PreconditionFailedException>()).Which.ErrorCode
                .Should().Be("common.precondition-failed");
        }

        var persisted = await PersistedWorkspace(seeded.WorkspaceId);
        persisted.Name.Should().Be("Renamed by pipeline",
            "the losing write must not overwrite the committed state");

        await using var probe = _db.CreateContext(SystemTenant());
        (await probe.Set<MessagingOutboxMessage>().CountAsync(m => m.AggregateId == seeded.WorkspaceId))
            .Should().Be(0, "a rolled-back request must not leave durable side-effect intents");
    }

    // --- session helpers -----------------------------------------------------

    private EfRequestDataSession CreateSession(ApplicationDbContext context) =>
        new(context, Mock.Of<IRlsSessionContext>(), Mock.Of<ILogger<EfRequestDataSession>>());

    private static RequestDataSessionOptions Options(ExpectedVersionConstraint constraint) =>
        new(RequestDataAccess.Transactional,
            ApplyTenantScope: false,
            ApplyResourceScope: false,
            ExpectedVersion: constraint);

    private static ExpectedVersionConstraint Constraint<TRequest>(Guid resourceId, long version)
        where TRequest : notnull =>
        new(typeof(TRequest),
            ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), resourceId),
            version);

    private async Task<Domain.Workspaces.Workspaces.Workspace> PersistedWorkspace(Guid id)
    {
        await using var verify = _db.CreateContext(SystemTenant());
        return await verify.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == id);
    }

    private async Task<(Domain.Workspaces.Workspaces.Workspace Workspace, Guid OwnerId)> SeedWorkspaceAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"ev-{Guid.NewGuid():N}@example.com", "EV User", "hashed", now, true);
        var account = Account.Create("EV Account", $"ev-{Guid.NewGuid():N}", AccountType.Team, ownerId, now);
        var workspace = Domain.Workspaces.Workspaces.Workspace.Create(
            account.Id, ownerId, "Concurrency Workspace", $"ev-{Guid.NewGuid():N}", now);
        var member = Domain.Workspaces.Members.WorkspaceMember.Create(
            account.Id, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)workspace).ClearDomainEvents();
        return (workspace, ownerId);
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid WorkspaceId)> SeedFullGraphAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"evp-{Guid.NewGuid():N}@example.com", "EV Pipeline User", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var account = Account.Create("EV Pipeline Account", $"evp-{Guid.NewGuid():N}", AccountType.Team, ownerId, now);
        var workspace = Domain.Workspaces.Workspaces.Workspace.Create(
            account.Id, ownerId, "Pipeline Workspace", $"evp-{Guid.NewGuid():N}", now);
        var member = Domain.Workspaces.Members.WorkspaceMember.Create(
            account.Id, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        await seed.SaveChangesAsync();

        await using var grants = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grants);
        await projection.SyncWorkspaceMemberGrantAsync(
            account.Id, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, now, CancellationToken.None);
        await grants.SaveChangesAsync();

        return (account.Id, ownerId, workspace.Id);
    }

    private ServiceProvider BuildPipelineProvider(Guid accountId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var environmentMock = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Testing");

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddOptions();
        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(userMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(environmentMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(UpdateWorkspaceProfileCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ApplicationTracingBehavior<,>));
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
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IOptions<RlsOptions>>(Microsoft.Extensions.Options.Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                new PostgresPageAuthorizationFacts(sp.GetRequiredService<ApplicationDbContext>())));
        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();
        services.AddScoped<ExecutionContextClass>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<ExecutionContextClass>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<ExecutionContextClass>());

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

        services.AddTransient<
            IRequestHandler<UpdateWorkspaceProfileCommand, Notrelix.Application.Common.Models.Result>,
            UpdateWorkspaceProfileCommandHandler>();

        return services.BuildServiceProvider();
    }

    private FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private static void SetEntityId(object entity, Guid id) =>
        entity.GetType()
            .GetProperty("Id",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!
            .SetValue(entity, id);

    private sealed record UnmappedProbeRequest : Notrelix.Application.Common.Requests.IExpectedVersionRequest
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), Guid.NewGuid());
        public long ExpectedVersion => 1;
    }
}
