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
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
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
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeamMembers;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;
using Notrelix.Domain.Workspaces.Teams;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Team administration reads used by the web Teams surface, on PostgreSQL
/// through the real pipeline (execution context, data session transaction,
/// access control):
/// - GetTeam and the workspace Team list report the aggregate version and the
///   status, and the member count agrees with the member list;
/// - GetTeamMembers lists only active members with their role: a removed member
///   no longer participates (WSP-014);
/// - a Team of another workspace is NotFound, and a caller outside the
///   workspace is denied.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class TeamAdministrationReadIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset BaseTime = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DateTimeOffset _now = BaseTime;

    public TeamAdministrationReadIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTeamMembers_ListsOnlyActiveMembers_AndTheTeamReadsAgree()
    {
        var stack = await SeedWorkspaceStackAsync();
        var leaver = await AddWorkspaceMemberAsync(stack);
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        _now = BaseTime.AddMinutes(1);
        await SendAsync(owner, new AddTeamMemberCommand(stack.WorkspaceId, teamId, stack.MemberId, "Member", (await PersistedTeamAsync(teamId)).Version));
        await SendAsync(owner, new AddTeamMemberCommand(stack.WorkspaceId, teamId, leaver, "Member", (await PersistedTeamAsync(teamId)).Version));
        _now = BaseTime.AddMinutes(2);
        await SendAsync(owner, new RemoveTeamMemberCommand(stack.WorkspaceId, teamId, leaver, (await PersistedTeamAsync(teamId)).Version));

        var members = await GetTeamMembersAsync(owner, stack.WorkspaceId, teamId);
        var team = await GetTeamAsync(owner, stack.WorkspaceId, teamId);
        var listed = await ListTeamsAsync(owner, stack.WorkspaceId);

        members.Select(m => (m.UserId, m.Role, m.Status)).Should().BeEquivalentTo(new[]
        {
            (stack.OwnerId, "Lead", "Active"),
            (stack.MemberId, "Member", "Active"),
        });
        members.Should().NotContain(m => m.UserId == leaver, "removal ends the participation");
        members.Should().OnlyContain(m => m.DisplayName.StartsWith("WX "), "names come from the user join");
        team.MemberCount.Should().Be(members.Count, "the count agrees with the list");
        listed.Single(t => t.Id == teamId).MemberCount.Should().Be(members.Count);
    }

    [Fact]
    public async Task GetTeam_ReportsTheAggregateVersionAndStatus()
    {
        var stack = await SeedWorkspaceStackAsync();
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        _now = BaseTime.AddMinutes(1);
        await SendAsync(owner, new AddTeamMemberCommand(stack.WorkspaceId, teamId, stack.MemberId, "Member", (await PersistedTeamAsync(teamId)).Version));

        var team = await GetTeamAsync(owner, stack.WorkspaceId, teamId);
        var listed = (await ListTeamsAsync(owner, stack.WorkspaceId)).Single(t => t.Id == teamId);

        var persisted = await PersistedTeamAsync(teamId);
        team.Version.Should().Be(persisted.Version, "the read reports the persisted aggregate version")
            .And.BeGreaterThan(1, "adding a member advances the version");
        listed.Version.Should().Be(persisted.Version);
        team.Status.Should().Be("Active");
        listed.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetTeamMembers_ForATeamOfAnotherWorkspace_IsNotFound()
    {
        var stack = await SeedWorkspaceStackAsync();
        var other = await SeedWorkspaceStackAsync();
        using var otherOwner = CreateProvider(other.AccountId, other.OwnerId);
        var foreignTeamId = await CreateTeamAsync(otherOwner, other.WorkspaceId, "Foreign");
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var read = () => GetTeamMembersAsync(owner, stack.WorkspaceId, foreignTeamId);

        await read.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTeamMembers_IsDeniedToANonMember()
    {
        var stack = await SeedWorkspaceStackAsync();
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = await CreateTeamAsync(owner, stack.WorkspaceId, "Platform");
        var outsider = await SeedUserAsync();
        using var provider = CreateProvider(stack.AccountId, outsider);

        var read = () => GetTeamMembersAsync(provider, stack.WorkspaceId, teamId);

        await read.Should().ThrowAsync<AppForbidden>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<object?> SendAsync(ServiceProvider provider, object request)
    {
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        if (result is Result plain)
            plain.Succeeded.Should().BeTrue(string.Join("; ", plain.Errors));
        return result;
    }

    private static async Task<Guid> CreateTeamAsync(ServiceProvider provider, Guid workspaceId, string name) =>
        ((Result<Guid>)(await SendAsync(provider, new CreateTeamCommand(workspaceId, name, null)))!).Data;

    private static async Task<TeamDto> GetTeamAsync(ServiceProvider provider, Guid workspaceId, Guid teamId) =>
        ((Result<TeamDto>)(await SendAsync(provider, new GetTeamQuery(workspaceId, teamId)))!).Data!;

    private static async Task<List<TeamDto>> ListTeamsAsync(ServiceProvider provider, Guid workspaceId) =>
        ((Result<List<TeamDto>>)(await SendAsync(provider, new GetWorkspaceTeamsQuery(workspaceId)))!).Data!;

    private static async Task<IReadOnlyList<TeamMemberDto>> GetTeamMembersAsync(ServiceProvider provider, Guid workspaceId, Guid teamId) =>
        (IReadOnlyList<TeamMemberDto>)(await SendAsync(provider, new GetTeamMembersQuery(workspaceId, teamId)))!;

    private async Task<(long Version, TeamStatus Status)> PersistedTeamAsync(Guid teamId)
    {
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var team = await verify.Teams.Where(t => t.Id == teamId).Select(t => new { t.Version, t.Status }).SingleAsync();
        return (team.Version, team.Status);
    }

    private async Task<Guid> AddWorkspaceMemberAsync((Guid AccountId, Guid OwnerId, Guid MemberId, Guid WorkspaceId) stack)
    {
        var userId = await SeedUserAsync();
        var member = WorkspaceMember.Create(stack.AccountId, stack.WorkspaceId, userId, WorkspaceRole.Member, stack.OwnerId, BaseTime);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.AccountMembers.Add(AccountMember.Create(stack.AccountId, userId, AccountRole.Member, stack.OwnerId, BaseTime));
        seed.WorkspaceMembers.Add(member);
        await seed.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> SeedUserAsync()
    {
        var user = User.Create($"wx-team-{Guid.NewGuid():N}@example.com", "WX Person", "hashed", BaseTime, true);
        user.ConfirmEmail(user.Id, BaseTime);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        return user.Id;
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid MemberId, Guid WorkspaceId)> SeedWorkspaceStackAsync()
    {
        var now = BaseTime;
        var owner = User.Create($"wx-team-{Guid.NewGuid():N}@example.com", "WX Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"wx-team-{Guid.NewGuid():N}@example.com", "WX Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("WX Team Account", $"wx-team-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "WX Team Workspace", $"wx-team-ws-{Guid.NewGuid():N}", now);

        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Users.Add(owner);
            seed.Users.Add(member);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
            seed.AccountMembers.Add(AccountMember.Create(account.Id, member.Id, AccountRole.Member, owner.Id, now));
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
            await seed.SaveChangesAsync();
        }

        await using var grant = _db.CreateOwnerContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return (account.Id, owner.Id, member.Id, workspace.Id);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    /// <summary>
    /// The production pipeline slice used by the Workspaces integration suites,
    /// with the real <see cref="EfRequestDataSession"/> (expected-version
    /// constraint), access-control engine and actor lookup.
    /// </summary>
    private ServiceProvider CreateProvider(Guid accountId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.RequireWorkspaceId()).Returns(() => tenant.WorkspaceId ?? Guid.Empty);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(() => _now);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(GetTeamQuery).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateOwnerContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
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
        services.AddScoped<IActorLookupService, ActorLookupService>();
        services.AddScoped<Notrelix.Application.Features.Identity.Public.Queries.IIdentityUserFacts, Notrelix.Application.Features.Identity.Users.Services.IdentityUserFactsProvider>();
        services.AddScoped<Notrelix.Application.Features.Workspaces.Public.Members.IMemberEmailDisclosure, Notrelix.Application.Features.Workspaces.Members.Services.MemberEmailDisclosure>();
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

        services.AddScoped<IRequestHandler<CreateTeamCommand, Result<Guid>>, CreateTeamCommandHandler>();
        services.AddScoped<IRequestHandler<AddTeamMemberCommand, Result>, AddTeamMemberCommandHandler>();
        services.AddScoped<IRequestHandler<RemoveTeamMemberCommand, Result>, RemoveTeamMemberCommandHandler>();
        services.AddScoped<IRequestHandler<GetTeamQuery, Result<TeamDto>>, GetTeamQueryHandler>();
        services.AddScoped<IRequestHandler<GetWorkspaceTeamsQuery, Result<List<TeamDto>>>, GetWorkspaceTeamsQueryHandler>();
        services.AddScoped<IRequestHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberDto>>, GetTeamMembersQueryHandler>();

        return services.BuildServiceProvider();
    }
}
