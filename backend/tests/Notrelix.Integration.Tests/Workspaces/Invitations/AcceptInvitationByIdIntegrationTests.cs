using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using ExecutionContextClass = Notrelix.Application.Common.Context.ExecutionContext;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Services;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces.Invitations;

/// <summary>
/// IA-TST-INV-ACCEPT-BY-ID — by-id invitation acceptance across the composed
/// production graph: handler → shared acceptance service → membership
/// provisioning → grant projection, committed atomically on real PostgreSQL.
/// Covers success, idempotent consume for existing active members, and
/// side-effect-free rejection for suspended members.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AcceptInvitationByIdIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AcceptInvitationByIdIntegrationTests(PostgresTestContainer db)
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
    public async Task AcceptInvitationById_WhenInviteeHasNoMembership_CreatesMember_ProvisionsAccount_WritesGrants_AndConsumesInvitation()
    {
        var seeded = await SeedInvitationGraphAsync(withMember: false);
        using var provider = BuildPipelineProvider(
            seeded.AccountId, seeded.WorkspaceId, seeded.InviteeId);

        var result = await SendAcceptAsync(provider, seeded.InvitationId);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.WorkspaceId.Should().Be(seeded.WorkspaceId);
        result.Data!.WorkspaceSlug.Should().Be(seeded.WorkspaceSlug);

        await using var verify = _db.CreateContext(SystemTenant());
        var invitation = await verify.Set<WorkspaceInvitation>()
            .IgnoreQueryFilters().SingleAsync(i => i.Id == seeded.InvitationId);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);

        var member = await verify.Set<WorkspaceMember>()
            .IgnoreQueryFilters().SingleAsync(m => m.WorkspaceId == seeded.WorkspaceId && m.UserId == seeded.InviteeId);
        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.Role.Should().Be(WorkspaceRole.Member);

        var accountMember = await verify.AccountMembers
            .IgnoreQueryFilters().SingleAsync(m => m.AccountId == seeded.AccountId && m.UserId == seeded.InviteeId);
        accountMember.Role.Should().Be(AccountRole.Member);

        var grant = await verify.AccessGrants
            .FirstOrDefaultAsync(g =>
                g.AccountId == seeded.AccountId
                && g.WorkspaceId == seeded.WorkspaceId
                && g.UserId == seeded.InviteeId);
        grant.Should().NotBeNull("the acceptance must project the member grant durably");
        grant!.MembershipStatus.Should().Be("Active");
    }

    [Fact]
    public async Task AcceptInvitationById_WhenInviteeIsAlreadyActiveMember_ConsumesIdempotently_WithoutDuplicateRoleOrGrant()
    {
        var seeded = await SeedInvitationGraphAsync(withMember: true);
        using var provider = BuildPipelineProvider(
            seeded.AccountId, seeded.WorkspaceId, seeded.InviteeId);

        var result = await SendAcceptAsync(provider, seeded.InvitationId);

        result.Succeeded.Should().BeTrue();

        await using var verify = _db.CreateContext(SystemTenant());
        var invitation = await verify.Set<WorkspaceInvitation>()
            .IgnoreQueryFilters().SingleAsync(i => i.Id == seeded.InvitationId);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);

        var members = verify.Set<WorkspaceMember>()
            .IgnoreQueryFilters().Where(m => m.WorkspaceId == seeded.WorkspaceId && m.UserId == seeded.InviteeId);
        members.Count().Should().Be(1, "an existing active member must not be duplicated");
        (await members.SingleAsync()).Role.Should().Be(WorkspaceRole.Member);

        var grants = verify.AccessGrants.Where(g =>
            g.AccountId == seeded.AccountId
            && g.WorkspaceId == seeded.WorkspaceId
            && g.UserId == seeded.InviteeId);
        grants.Count().Should().Be(1, "the grant must not be re-projected for an already active member");
    }

    [Fact]
    public async Task AcceptInvitationById_WhenExistingMemberIsSuspended_Rejects_WithoutMutatingInvitationOrWrites()
    {
        var seeded = await SeedInvitationGraphAsync(withMember: true, suspended: true);
        using var provider = BuildPipelineProvider(
            seeded.AccountId, seeded.WorkspaceId, seeded.InviteeId);

        var result = await SendAcceptAsync(provider, seeded.InvitationId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("suspended"));

        await using var verify = _db.CreateContext(SystemTenant());
        var invitation = await verify.Set<WorkspaceInvitation>()
            .IgnoreQueryFilters().SingleAsync(i => i.Id == seeded.InvitationId);
        invitation.Status.Should().Be(
            WorkspaceInvitationStatus.Pending,
            "rejected acceptance must leave the invitation pending and side-effect free");

        var member = await verify.Set<WorkspaceMember>()
            .IgnoreQueryFilters().SingleAsync(m => m.WorkspaceId == seeded.WorkspaceId && m.UserId == seeded.InviteeId);
        member.Status.Should().Be(WorkspaceMemberStatus.Suspended);

        var grants = verify.AccessGrants.Where(g =>
            g.AccountId == seeded.AccountId
            && g.WorkspaceId == seeded.WorkspaceId
            && g.UserId == seeded.InviteeId);
        grants.Count().Should().Be(0, "a rejected acceptance must not project grants");

        var accountMembers = verify.AccountMembers.Where(m =>
            m.AccountId == seeded.AccountId && m.UserId == seeded.InviteeId);
        accountMembers.Count().Should().Be(1, "a rejected acceptance must not add account membership");
        (await accountMembers.SingleAsync()).Role.Should().Be(
            AccountRole.Member, "the pre-existing account membership must not be overwritten");
    }

    // --- helpers -------------------------------------------------------------

    private async Task<Result<AcceptInvitationResultDto>> SendAcceptAsync(
        ServiceProvider provider, Guid invitationId)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new AcceptInvitationByIdCommand(invitationId));
    }

    private async Task<SeededGraph> SeedInvitationGraphAsync(bool withMember, bool suspended = false)
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var workspaceSlug = $"iv-{Guid.NewGuid():N}";

        var owner = User.Create($"owner-{Guid.NewGuid():N}@example.com", "Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var invitee = User.Create($"invitee-{Guid.NewGuid():N}@example.com", "Invitee", "hashed", now, true);
        invitee.ConfirmEmail(invitee.Id, now);

        var account = Account.Create("Invitation Account", $"ia-{Guid.NewGuid():N}", AccountType.Team, ownerId, now);
        var workspace = Notrelix.Domain.Workspaces.Workspaces.Workspace.Create(
            account.Id, ownerId, "Invitation Workspace", workspaceSlug, now);
        var ownerMember = WorkspaceMember.Create(
            account.Id, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now);

        var invitation = WorkspaceInvitation.Create(
            account.Id,
            workspace.Id,
            invitee.Email.Value,
            WorkspaceRole.Member,
            InvitationTokenHash.Create(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")),
            1,
            ownerId,
            now,
            TimeSpan.FromDays(7));

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.AddRange(owner, invitee);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, now));
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(ownerMember);
        seed.Set<WorkspaceInvitation>().Add(invitation);

        if (withMember)
        {
            var inviteeMember = WorkspaceMember.Create(
                account.Id, workspace.Id, invitee.Id, WorkspaceRole.Member, ownerId, now);
            if (suspended)
            {
                inviteeMember.Suspend(ownerId, now, activeOwnerCount: 1);
            }

            seed.WorkspaceMembers.Add(inviteeMember);
            seed.AccountMembers.Add(AccountMember.Create(
                account.Id, invitee.Id, AccountRole.Member, ownerId, now));
        }

        await seed.SaveChangesAsync();

        await using var grants = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grants);
        await projection.SyncWorkspaceMemberGrantAsync(
            account.Id, workspace.Id, ownerId, WorkspaceRole.Owner, now, CancellationToken.None);
        if (withMember && !suspended)
        {
            await projection.SyncWorkspaceMemberGrantAsync(
                account.Id, workspace.Id, invitee.Id, WorkspaceRole.Member, now, CancellationToken.None);
        }

        await grants.SaveChangesAsync();

        ((IHasDomainEvents)workspace).ClearDomainEvents();
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        return new SeededGraph(
            account.Id, owner.Id, workspace.Id, workspace.Slug, invitee.Id, invitation.Id);
    }

    private ServiceProvider BuildPipelineProvider(Guid accountId, Guid workspaceId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, userId);

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
            RequestDescriptorRegistry.Create(typeof(AcceptInvitationByIdCommand).Assembly));

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
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
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
                sp.GetRequiredService<IResourceAuthorizationFactsProvider>()));
        services.AddScoped<IResourceAuthorizationFactsProvider, FakeResourceAuthorizationFactsProvider>();
        services.AddScoped<IAccessGrantProjectionService>(sp =>
            new AccessGrantProjectionService(sp.GetRequiredService<ApplicationDbContext>()));
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

        services.AddScoped<IIdentityUserLookupService>(sp =>
            new IdentityUserLookupService(sp.GetRequiredService<IIdentityDbContext>()));
        services.AddScoped<IAccountStatusReader>(sp =>
            new AccountStatusReader(sp.GetRequiredService<IAccountDbContext>()));
        services.AddScoped<IAccountMembershipProvisioner>(sp =>
            new AccountMembershipProvisioner(
                sp.GetRequiredService<IAccountDbContext>(),
                sp.GetRequiredService<IAccessGrantProjectionService>()));
        services.AddScoped<IInvitationAcceptanceService>(sp =>
            new InvitationAcceptanceService(
                sp.GetRequiredService<IWorkspaceDbContext>(),
                sp.GetRequiredService<IIdentityUserLookupService>(),
                sp.GetRequiredService<IAccountMembershipProvisioner>(),
                sp.GetRequiredService<IAccountStatusReader>(),
                sp.GetRequiredService<IDateTimeProvider>(),
                sp.GetRequiredService<IAccessGrantProjectionService>()));

        services.AddTransient<
            IRequestHandler<AcceptInvitationByIdCommand, Result<AcceptInvitationResultDto>>,
            AcceptInvitationByIdCommandHandler>();

        return services.BuildServiceProvider();
    }

    private FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed record SeededGraph(
        Guid AccountId,
        Guid OwnerId,
        Guid WorkspaceId,
        string WorkspaceSlug,
        Guid InviteeId,
        Guid InvitationId);
}