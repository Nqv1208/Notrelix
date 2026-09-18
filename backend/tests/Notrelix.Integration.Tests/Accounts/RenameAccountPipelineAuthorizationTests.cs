using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Accounts;

/// <summary>
/// Production-graph proof that Account rename is authorized by the canonical
/// pipeline (real DataSessionBehavior + real AccessControlBehavior over real
/// PostgreSQL) BEFORE the handler executes. Admin is allowed; a plain Member
/// is denied with no durable mutation. The handler itself contains no role
/// branch; this test proves the runtime consequence.
/// </summary>
[Collection("Database")]
public class RenameAccountPipelineAuthorizationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RenameAccountPipelineAuthorizationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<AccountGraph> SeedAccountAsync(AccountRole role)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"rename-{Guid.NewGuid():N}@example.com", "Rename User", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var actorId = user.Id;
        var account = Account.Create("Rename Account", $"rename-{Guid.NewGuid():N}", AccountType.Team, actorId, now);
        var accountId = account.Id;

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(user);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(accountId, actorId, role, actorId, now));
            await seed.SaveChangesAsync();
        }

        return new AccountGraph(accountId, actorId);
    }

    [Fact]
    public async Task RenameAccount_AdminThroughPipeline_RenamesAccount()
    {
        var graph = await SeedAccountAsync(AccountRole.Admin);

        using var provider = CreatePipelineProvider(graph.AccountId, graph.ActorId);
        var sender = provider.GetRequiredService<ISender>();
        var evaluations = provider.GetRequiredService<EvaluationCountingDecisionStore>();
        var command = new RenameAccountCommand("Renamed By Admin");

        var result = await sender.Send(command);

        result.Succeeded.Should().BeTrue("frozen baseline allows Admin RenameAccount through pipeline authorization");
        evaluations.EvaluationCount.Should().Be(1,
            "pipeline-owned authorization must be evaluated exactly once per request");

        await using var verify = _db.CreateContext(SystemTenant());
        var account = await verify.Accounts.SingleAsync(a => a.Id == graph.AccountId);
        account.Name.Should().Be("Renamed By Admin", "the handler must have executed after pipeline authorization succeeded");
    }

    [Fact]
    public async Task RenameAccount_MemberThroughPipeline_IsDeniedWithoutMutation()
    {
        var graph = await SeedAccountAsync(AccountRole.Member);

        using var provider = CreatePipelineProvider(graph.AccountId, graph.ActorId);
        var sender = provider.GetRequiredService<ISender>();
        var evaluations = provider.GetRequiredService<EvaluationCountingDecisionStore>();
        var command = new RenameAccountCommand("Renamed By Member");

        var act = () => sender.Send(command);

        await act.Should().ThrowAsync<AppForbidden>(
            "frozen baseline denies Member RenameAccount — the pipeline must deny before handler effects");
        evaluations.EvaluationCount.Should().Be(1,
            "a pipeline denial performs one evaluation and never reaches the handler");

        await using var verify = _db.CreateContext(SystemTenant());
        var account = await verify.Accounts.SingleAsync(a => a.Id == graph.AccountId);
        account.Name.Should().Be("Rename Account",
            "a denied request must leave no durable side effects");
    }

    /// <summary>
    /// Composes the production MediatR pipeline slice: the REAL DataSessionBehavior
    /// and REAL AccessControlBehavior registered exactly as production does, over the
    /// real PostgresAccessFactsProvider + pure policy evaluator and the REAL command handler.
    /// No authorization decision is duplicated or mocked away.
    /// </summary>
    private ServiceProvider CreatePipelineProvider(Guid accountId, Guid userId)
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

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(userMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(clockMock.Object);

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        var descriptors = RequestDescriptorRegistry.Create(typeof(RenameAccountCommand).Assembly);
        services.AddSingleton<IRequestDescriptorRegistry>(descriptors);
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(context => context.Snapshot).Returns(new ExecutionContextSnapshot(
            userId, accountId, null, null,
            ApplicationPrincipalKind.Authenticated,
            ApplicationScopeKind.Account,
            Guid.NewGuid().ToString("D")));
        services.AddSingleton(executionContext.Object);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddSingleton<EvaluationCountingDecisionStore>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            sp.GetRequiredService<EvaluationCountingDecisionStore>().Wrap(
                new PostgresAccessFactsProvider(
                    sp.GetRequiredService<ApplicationDbContext>(),
                    sp.GetRequiredService<TimeProvider>(),
                    new PostgresPageAuthorizationFacts(sp.GetRequiredService<ApplicationDbContext>()),
                    new FakeBillingSubscriptionFacts())));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        services.AddTransient<
            IRequestHandler<RenameAccountCommand, Notrelix.Application.Common.Models.Result>,
            RenameAccountCommandHandler>();

        return services.BuildServiceProvider();
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed record AccountGraph(Guid AccountId, Guid ActorId);

    /// <summary>
    /// Pass-through decorator over the REAL production decision store that observes
    /// how many times the pipeline evaluated authorization for a request without
    /// changing any decision, proving the handler performs no duplicate permission lookup.
    /// </summary>
    public sealed class EvaluationCountingDecisionStore
    {
        private int _evaluationCount;

        public IAccessFactsProvider Wrap(IAccessFactsProvider inner) =>
            new CountingInner(this, inner);

        public int EvaluationCount => _evaluationCount;

        private sealed class CountingInner : IAccessFactsProvider
        {
            private readonly EvaluationCountingDecisionStore _owner;
            private readonly IAccessFactsProvider _inner;

            public CountingInner(EvaluationCountingDecisionStore owner, IAccessFactsProvider inner)
            {
                _owner = owner;
                _inner = inner;
            }

            public async Task<AccessFacts> ResolveAsync(
                RequestDescriptor descriptor,
                ExecutionContextSnapshot context,
                object request,
                CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _owner._evaluationCount);
                return await _inner.ResolveAsync(descriptor, context, request, cancellationToken);
            }
        }
    }
}