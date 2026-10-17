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
using AccessFacts = Notrelix.Application.Common.Security.AccessFacts;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// WG-REL-001 / WG-TST-SEC-MASTER-003 — authorization dependency failure cannot
/// become an implicit allow. Through the production pipeline slice (real
/// DataSessionBehavior + real AccessControlBehavior over real PostgreSQL and the
/// real PostgresAccessFactsProvider + real AccessPolicyEngine), an armed fault in
/// either authorization dependency must abort the request before the protected
/// handler executes: the fault surfaces, no durable mutation occurs, and no
/// stale/broader authority is substituted. The un-faulted control proves the same
/// composition still allows an authorized actor, so the negatives are real
/// fail-closed outcomes and not a permanently-denying harness.
/// </summary>
[Collection("Database")]
public class AuthorizationFailClosedFaultInjectionTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AuthorizationFailClosedFaultInjectionTests(PostgresTestContainer db)
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
        var user = User.Create($"fault-{Guid.NewGuid():N}@example.com", "Fault User", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var actorId = user.Id;
        var account = Account.Create("Fault Account", $"fault-{Guid.NewGuid():N}", AccountType.Team, actorId, now);
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
    public async Task RenameAccount_FactsProviderThrows_HandlerDoesNotRunAndNoDurableMutation()
    {
        var graph = await SeedAccountAsync(AccountRole.Admin);

        using var provider = CreatePipelineProvider(graph.AccountId, graph.ActorId);
        var sender = provider.GetRequiredService<ISender>();
        var faultHost = provider.GetRequiredService<FaultHost>();
        faultHost.FactsFault = new InvalidOperationException("simulated access-facts provider outage");
        var command = new RenameAccountCommand("Renamed Despite Facts Failure");

        var act = () => sender.Send(command);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*access-facts provider outage*",
            "a facts-provider outage must surface, never be converted into an allow or a stale authority");
        faultHost.FactsCalls.Should().Be(1, "the outage is raised when the pipeline resolves datastore facts");
        faultHost.PolicyCalls.Should().Be(0, "no policy evaluation may occur after facts resolution failed");

        await using var verify = _db.CreateContext(SystemTenant());
        var account = await verify.Accounts.SingleAsync(a => a.Id == graph.AccountId);
        account.Name.Should().Be("Fault Account", "the protected handler must not run, so no durable side effect exists");
    }

    [Fact]
    public async Task RenameAccount_PolicyEvaluatorThrows_HandlerDoesNotRunAndNoDurableMutation()
    {
        var graph = await SeedAccountAsync(AccountRole.Admin);

        using var provider = CreatePipelineProvider(graph.AccountId, graph.ActorId);
        var sender = provider.GetRequiredService<ISender>();
        var faultHost = provider.GetRequiredService<FaultHost>();
        faultHost.PolicyFault = new InvalidOperationException("simulated access-policy evaluator outage");
        var command = new RenameAccountCommand("Renamed Despite Policy Failure");

        var act = () => sender.Send(command);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*access-policy evaluator outage*",
            "a policy-evaluator outage must surface, never fall through to an implicit allow");
        faultHost.FactsCalls.Should().Be(1, "facts resolve normally before the evaluator fault is raised");
        faultHost.PolicyCalls.Should().Be(1, "the evaluator fault is raised at the single policy decision point");

        await using var verify = _db.CreateContext(SystemTenant());
        var account = await verify.Accounts.SingleAsync(a => a.Id == graph.AccountId);
        account.Name.Should().Be("Fault Account", "the protected handler must not run, so no durable side effect exists");
    }

    [Fact]
    public async Task RenameAccount_NoFault_AdminStillSucceedsThroughSameComposition()
    {
        var graph = await SeedAccountAsync(AccountRole.Admin);

        using var provider = CreatePipelineProvider(graph.AccountId, graph.ActorId);
        var sender = provider.GetRequiredService<ISender>();
        var faultHost = provider.GetRequiredService<FaultHost>();
        var command = new RenameAccountCommand("Renamed By Admin");

        var result = await sender.Send(command);

        result.Succeeded.Should().BeTrue("the same production pipeline must allow an authorized admin when no fault is armed");
        faultHost.FactsCalls.Should().Be(1, "authorization is evaluated exactly once per request");
        faultHost.PolicyCalls.Should().Be(1, "authorization is evaluated exactly once per request");

        await using var verify = _db.CreateContext(SystemTenant());
        var account = await verify.Accounts.SingleAsync(a => a.Id == graph.AccountId);
        account.Name.Should().Be("Renamed By Admin", "the un-faulted control proves the negative outcome is fail-closed, not a permanent deny");
    }

    /// <summary>
    /// Composes the production MediatR pipeline slice: the REAL DataSessionBehavior
    /// and REAL AccessControlBehavior over the real PostgresAccessFactsProvider and
    /// real AccessPolicyEngine, followed by the REAL command handler. A FaultHost
    /// sits between the pipeline and the real dependencies, so a controlled
    /// failure is injected through the production-equivalent DI seam without
    /// changing any decision or substituting any authority when no fault is armed.
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

        var faultHost = new FaultHost();
        services.AddSingleton(faultHost);
        services.AddScoped<IAccessFactsProvider>(sp =>
            faultHost.WrapFacts(
                new PostgresAccessFactsProvider(
                    sp.GetRequiredService<ApplicationDbContext>(),
                    sp.GetRequiredService<TimeProvider>(),
                    new PostgresPageAuthorizationFacts(sp.GetRequiredService<ApplicationDbContext>()),
                    new FakeBillingSubscriptionFacts())));
        services.AddSingleton<IAccessPolicyEvaluator>(sp =>
            faultHost.WrapPolicy(sp.GetRequiredService<AccessPolicyEngine>()));

        // Production pipeline nesting: DataSessionBehavior (outer) → AccessControlBehavior (inner).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        // The REAL production command handler.
        services.AddTransient<
            IRequestHandler<RenameAccountCommand, Notrelix.Application.Common.Models.Result>,
            RenameAccountCommandHandler>();
        services.AddSingleton<AccessPolicyEngine>();

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
    /// DI seam that injects a controlled failure into the authorization
    /// dependency surface (facts resolution and policy evaluation) and records
    /// how often each dependency was consulted. With no fault armed it is a
    /// pass-through, so the un-faulted control stays production-equivalent.
    /// </summary>
    public sealed class FaultHost
    {
        private readonly object _gate = new();
        private int _factsCalls;
        private int _policyCalls;

        public Exception? FactsFault { get; set; }
        public Exception? PolicyFault { get; set; }

        public int FactsCalls
        {
            get { lock (_gate) { return _factsCalls; } }
        }

        public int PolicyCalls
        {
            get { lock (_gate) { return _policyCalls; } }
        }

        public IAccessFactsProvider WrapFacts(IAccessFactsProvider inner) => new FaultyFacts(this, inner);

        public IAccessPolicyEvaluator WrapPolicy(IAccessPolicyEvaluator inner) => new FaultyPolicy(this, inner);

        private void RecordFactsCall()
        {
            lock (_gate)
            {
                _factsCalls++;
            }
        }

        private void RecordPolicyCall()
        {
            lock (_gate)
            {
                _policyCalls++;
            }
        }

        private sealed class FaultyFacts : IAccessFactsProvider
        {
            private readonly FaultHost _owner;
            private readonly IAccessFactsProvider _inner;

            public FaultyFacts(FaultHost owner, IAccessFactsProvider inner)
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
                _owner.RecordFactsCall();
                var fault = _owner.FactsFault;
                if (fault is not null)
                {
                    throw fault;
                }

                return await _inner.ResolveAsync(descriptor, context, request, cancellationToken);
            }
        }

        private sealed class FaultyPolicy : IAccessPolicyEvaluator
        {
            private readonly FaultHost _owner;
            private readonly IAccessPolicyEvaluator _inner;

            public FaultyPolicy(FaultHost owner, IAccessPolicyEvaluator inner)
            {
                _owner = owner;
                _inner = inner;
            }

            public AccessDecision Evaluate(
                RequestDescriptor descriptor,
                ExecutionContextSnapshot context,
                AccessFacts facts,
                object request)
            {
                _owner.RecordPolicyCall();
                var fault = _owner.PolicyFault;
                if (fault is not null)
                {
                    throw fault;
                }

                return _inner.Evaluate(descriptor, context, facts, request);
            }
        }
    }
}