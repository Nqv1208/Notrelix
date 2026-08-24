using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// IA-TST-X-AUTHZ-001 / IA-TST-PERF-001 / IAREQ090 / IAREQ136 / IAREQ138.
///
/// Production-graph proof that workspace creation is authorized by the canonical
/// pipeline (real AuthorizationBehavior + real PermissionService over real
/// PostgreSQL) BEFORE the handler executes, for representative Account roles.
/// The handler itself contains no role branch (enforced statically by
/// IA-TST-AUTHZ-ARCH-001..005); this test proves the runtime consequence.
/// </summary>
[Collection("Database")]
public class WorkspaceCreationPipelineAuthorizationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceCreationPipelineAuthorizationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(AccountRole.Owner, true)]
    [InlineData(AccountRole.Admin, true)]
    [InlineData(AccountRole.Member, false)]
    [InlineData(AccountRole.BillingAdmin, false)]
    [InlineData(AccountRole.SecurityAdmin, false)]
    public async Task CreateWorkspace_IsAuthorizedByPipeline_ForRepresentativeRoles(
        AccountRole role,
        bool expectedAllowed)
    {
        // Seed account + member through the real DbContext.
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"pipeline-{Guid.NewGuid():N}@example.com", "Pipeline User", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var actorId = user.Id;
        var account = Account.Create("Pipeline Account", $"pipeline-{Guid.NewGuid():N}", AccountType.Team, actorId, now);
        var accountId = account.Id;

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(user);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(accountId, actorId, role, actorId, now));
            await seed.SaveChangesAsync();
        }

        using var provider = CreatePipelineProvider(accountId, actorId);
        var sender = provider.GetRequiredService<ISender>();
        var evaluations = provider.GetRequiredService<EvaluationCountingDecisionStore>();
        var command = new CreateWorkspaceCommand($"WS {role}", null, false);

        if (expectedAllowed)
        {
            var result = await sender.Send(command);

            result.Succeeded.Should().BeTrue(
                $"frozen baseline allows {role} CreateWorkspace through pipeline authorization");

            // IA-TST-PERF-001: exactly one pipeline authorization evaluation —
            // the handler must not repeat the permission lookup.
            evaluations.EvaluationCount.Should().Be(1,
                "pipeline-owned authorization must be evaluated exactly once per request");

            await using var verify = _db.CreateContext(SystemTenant());
            var workspace = await verify.Workspaces.FirstOrDefaultAsync(w => w.Id == result.Data);
            workspace.Should().NotBeNull("the handler must have executed after pipeline authorization succeeded");
        }
        else
        {
            var act = () => sender.Send(command);

            var assertion = await act.Should().ThrowAsync<AppForbidden>(
                $"frozen baseline denies {role} CreateWorkspace — the pipeline must deny before handler effects");

            // Denied requests are also evaluated exactly once, before the handler.
            evaluations.EvaluationCount.Should().Be(1,
                "a pipeline denial performs one evaluation and never reaches the handler");

            // No workspace may exist after the denied request.
            await using var verify = _db.CreateContext(SystemTenant());
            verify.Workspaces.Any(w => w.Name == $"WS {role}").Should().BeFalse(
                "a denied request must leave no durable side effects");
        }
    }

    /// <summary>
    /// Composes the production MediatR pipeline slice: the REAL AuthorizationBehavior
    /// registered exactly as production does, delegating to the REAL PermissionService
    /// evaluator over the test PostgreSQL graph, followed by the REAL command handler.
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
        // Register MediatR core without assembly scanning — the pipeline under test
        // is composed explicitly below from production behavior + production handler.
        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(userMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(clockMock.Object);

        // Production data graph: one scoped DbContext serves the request session,
        // the evaluator and the handler exactly as the production composition does.
        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccessGrantProjectionService>(sp =>
            new AccessGrantProjectionService(sp.GetRequiredService<ApplicationDbContext>()));

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        var descriptors = RequestDescriptorRegistry.Create(typeof(CreateWorkspaceCommand).Assembly);
        services.AddSingleton<IRequestDescriptorRegistry>(descriptors);
        var executionContext = new Mock<IExecutionContextReader>();
        executionContext.SetupGet(context => context.Snapshot).Returns(new ExecutionContextSnapshot(
            userId, accountId, null, null,
            ApplicationPrincipalKind.Authenticated,
            ApplicationScopeKind.Account,
            Guid.NewGuid().ToString("D")));
        services.AddSingleton(executionContext.Object);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddSingleton<EvaluationCountingDecisionStore>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            sp.GetRequiredService<EvaluationCountingDecisionStore>().Wrap(
                new PostgresAccessFactsProvider(
                    sp.GetRequiredService<ApplicationDbContext>(),
                    sp.GetRequiredService<TimeProvider>())));

        // Production pipeline nesting: DbRequestScopeBehavior (outer) → AuthorizationBehavior (inner),
        // matching the canonical behavior order.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));

        // The REAL production command handler.
        services.AddTransient<
            IRequestHandler<CreateWorkspaceCommand, Notrelix.Application.Common.Models.Result<Guid>>,
            CreateWorkspaceCommandHandler>();

        return services.BuildServiceProvider();
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    /// <summary>
    /// IA-TST-PERF-001 instrumentation: pass-through decorator over the REAL
    /// production decision store. It observes how many times the pipeline
    /// evaluated authorization for a request without changing any decision,
    /// proving the handler performs no duplicate permission lookup.
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
