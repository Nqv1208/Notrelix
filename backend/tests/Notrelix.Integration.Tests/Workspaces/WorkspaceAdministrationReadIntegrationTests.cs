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
using Notrelix.Application.Common.Requests;
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
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaceMembership;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetUserWorkspaces;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Workspace administration reads used by the web Members, Settings and Home
/// surfaces, on PostgreSQL through the real pipeline (execution context, data
/// session transaction with the expected-version constraint, access control):
/// - GetWorkspace and the workspace list report the aggregate version, and that
///   version is what profile, settings and archive writes accept; a stale one is
///   rejected (412) and leaves the workspace unchanged;
/// - ListMembers reports each member's status and never lists a removed member
///   (removal ends participation, docs/product/workspaces.md §13, §20), the member
///   count agrees with the list, it pages by cursor so every member is listed
///   exactly once even when they joined at the same instant, and it is refused to a
///   person who is not an active member of that workspace;
/// - GetMyMembership returns the caller's own row and nobody else's;
/// - ListWorkspaceInvitations reports each invitation's status, so a revoked or
///   expired invitation is not presented as pending.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceAdministrationReadIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset BaseTime = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DateTimeOffset _now = BaseTime;

    public WorkspaceAdministrationReadIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Version ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkspace_ReportsTheVersionThatWritesAccept_AndAStaleVersionIsRejectedWithoutChange()
    {
        var stack = await SeedWorkspaceStackAsync();
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var initial = await GetWorkspaceAsync(owner, stack.WorkspaceId);
        initial.Version.Should().BeGreaterThan(0);
        initial.Version.Should().Be(await PersistedVersionAsync(stack.WorkspaceId), "the read reports the persisted aggregate version");

        _now = BaseTime.AddMinutes(1);
        await SendAsync(owner, new UpdateWorkspaceProfileCommand(stack.WorkspaceId, "Renamed WS", "About us", initial.Version));
        var renamed = await GetWorkspaceAsync(owner, stack.WorkspaceId);
        renamed.Name.Should().Be("Renamed WS");
        renamed.Description.Should().Be("About us");
        renamed.Version.Should().BeGreaterThan(initial.Version);

        var staleProfile = () => SendAsync(owner, new UpdateWorkspaceProfileCommand(stack.WorkspaceId, "Lost update", null, initial.Version));
        await staleProfile.Should().ThrowAsync<PreconditionFailed>();
        var afterStale = await GetWorkspaceAsync(owner, stack.WorkspaceId);
        afterStale.Name.Should().Be("Renamed WS", "a rejected stale write leaves the workspace unchanged");
        afterStale.Version.Should().Be(renamed.Version);

        _now = BaseTime.AddMinutes(2);
        await SendAsync(owner, new UpdateWorkspaceSettingsCommand(stack.WorkspaceId, true, false, true, "Guest", 14, renamed.Version));
        var afterSettings = await GetWorkspaceAsync(owner, stack.WorkspaceId);
        afterSettings.Version.Should().BeGreaterThan(renamed.Version);

        var listed = await ListUserWorkspacesAsync(owner);
        listed.Should().ContainSingle(w => w.Id == stack.WorkspaceId)
            .Which.Version.Should().Be(afterSettings.Version, "the workspace list reports the same version as the detail read");

        var staleArchive = () => SendAsync(owner, new ArchiveWorkspaceCommand(stack.WorkspaceId, renamed.Version));
        await staleArchive.Should().ThrowAsync<PreconditionFailed>();
        (await PersistedStatusAsync(stack.WorkspaceId)).Should().Be(WorkspaceStatus.Active);

        _now = BaseTime.AddMinutes(3);
        await SendAsync(owner, new ArchiveWorkspaceCommand(stack.WorkspaceId, afterSettings.Version));
        (await PersistedStatusAsync(stack.WorkspaceId)).Should().Be(WorkspaceStatus.Archived);
    }

    // ── Members ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListMembers_ReportsStatus_AndNeverListsARemovedMember()
    {
        var stack = await SeedWorkspaceStackAsync();
        var suspendedId = await AddMemberAsync(stack, member => member.Suspend(stack.OwnerId, BaseTime, activeOwnerCount: 1));
        var removedId = await AddMemberAsync(stack, member => member.Remove(activeOwnerCount: 1, stack.OwnerId, BaseTime));
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var members = await ListMembersAsync(owner, stack.WorkspaceId);

        members.Select(m => (m.UserId, m.Status)).Should().BeEquivalentTo(new[]
        {
            (stack.OwnerId, "Active"),
            (stack.MemberId, "Active"),
            (suspendedId, "Suspended"),
        });
        members.Should().NotContain(m => m.UserId == removedId, "removal ends the participation");
        (await GetWorkspaceAsync(owner, stack.WorkspaceId)).MemberCount.Should().Be(3, "the count agrees with the list");
        (await ListUserWorkspacesAsync(owner)).Single(w => w.Id == stack.WorkspaceId).MemberCount.Should().Be(3);
    }

    [Fact]
    public async Task ListMembers_IsDeniedToANonMember()
    {
        var stack = await SeedWorkspaceStackAsync();
        var outsider = await SeedUserAsync();
        using var provider = CreateProvider(stack.AccountId, outsider);

        var list = () => ListMembersAsync(provider, stack.WorkspaceId);

        await list.Should().ThrowAsync<AppForbidden>();
    }

    [Fact]
    public async Task ListMembers_WalksEveryMemberOnceThroughCursorPages_AndNeverReachesAnotherWorkspace()
    {
        // Everyone below joined at the same instant, so only the user id orders them.
        var stack = await SeedWorkspaceStackAsync();
        var other = await SeedWorkspaceStackAsync();
        var added = new[]
        {
            await AddMemberAsync(stack, _ => { }),
            await AddMemberAsync(stack, _ => { }),
            await AddMemberAsync(stack, _ => { }),
        };
        await AddMemberAsync(stack, member => member.Remove(activeOwnerCount: 1, stack.OwnerId, BaseTime));
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var seen = new List<Guid>();
        var sizes = new List<int>();
        string? cursor = null;
        do
        {
            var page = await ListMembersPageAsync(owner, stack.WorkspaceId, cursor, limit: 2);
            sizes.Add(page.Items.Length);
            seen.AddRange(page.Items.Select(m => m.UserId));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        sizes.Should().Equal(2, 2, 1);
        seen.Should().BeEquivalentTo(new[] { stack.OwnerId, stack.MemberId }.Concat(added), "each person once, the removed one never");
        seen.Should().NotContain(other.OwnerId).And.NotContain(other.MemberId);
    }

    [Fact]
    public async Task ListMembers_DefaultsToFiftyPerPage()
    {
        var stack = await SeedWorkspaceStackAsync();
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var page = await ListMembersPageAsync(owner, stack.WorkspaceId);

        page.Items.Should().HaveCount(2);
        page.NextCursor.Should().BeNull("a short list is one page");
    }

    [Fact]
    public async Task ListMembers_IsDeniedToASuspendedMember_AndToOneWhoWasRemoved()
    {
        var stack = await SeedWorkspaceStackAsync();
        var suspended = await AddMemberAsync(stack, member => member.Suspend(stack.OwnerId, BaseTime, activeOwnerCount: 1));
        var removed = await AddMemberAsync(stack, member => member.Remove(activeOwnerCount: 1, stack.OwnerId, BaseTime));

        foreach (var user in new[] { suspended, removed })
        {
            using var provider = CreateProvider(stack.AccountId, user);
            var list = () => ListMembersAsync(provider, stack.WorkspaceId);
            await list.Should().ThrowAsync<AppForbidden>();
        }
    }

    [Fact]
    public async Task ListMembers_IsDeniedToAMemberOfAnotherWorkspace()
    {
        var stack = await SeedWorkspaceStackAsync();
        var other = await SeedWorkspaceStackAsync();
        using var asOtherOwner = CreateProvider(other.AccountId, other.OwnerId);

        var intoOther = () => ListMembersAsync(asOtherOwner, stack.WorkspaceId);

        await intoOther.Should().ThrowAsync<AppForbidden>();
        (await ListMembersAsync(asOtherOwner, other.WorkspaceId)).Select(m => m.UserId)
            .Should().BeEquivalentTo(new[] { other.OwnerId, other.MemberId });
    }

    [Fact]
    public async Task GetMyMembership_ReturnsTheCallersOwnRow_AndNobodyElses()
    {
        var stack = await SeedWorkspaceStackAsync();
        using var asMember = CreateProvider(stack.AccountId, stack.MemberId);

        var mine = await GetMyMembershipAsync(asMember, stack.WorkspaceId);

        mine.UserId.Should().Be(stack.MemberId);
        mine.Role.Should().Be("Member");
        mine.Status.Should().Be("Active");
        mine.Version.Should().Be(await PersistedMemberVersionAsync(stack.WorkspaceId, stack.MemberId), "the read reports the version a member write carries");
    }

    [Fact]
    public async Task GetMyMembership_IsDeniedToAMemberOfAnotherWorkspace_AndToASuspendedMember()
    {
        var stack = await SeedWorkspaceStackAsync();
        var other = await SeedWorkspaceStackAsync();
        var suspended = await AddMemberAsync(stack, member => member.Suspend(stack.OwnerId, BaseTime, activeOwnerCount: 1));
        using var asOtherOwner = CreateProvider(other.AccountId, other.OwnerId);
        using var asSuspended = CreateProvider(stack.AccountId, suspended);

        var fromOther = () => GetMyMembershipAsync(asOtherOwner, stack.WorkspaceId);
        var whileSuspended = () => GetMyMembershipAsync(asSuspended, stack.WorkspaceId);

        await fromOther.Should().ThrowAsync<AppForbidden>();
        await whileSuspended.Should().ThrowAsync<AppForbidden>();
    }

    // ── Invitations ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListWorkspaceInvitations_ReportsEachInvitationsStatus()
    {
        var stack = await SeedWorkspaceStackAsync();
        var pending = await SeedInvitationAsync(stack, "pending@example.com", _ => { });
        var revoked = await SeedInvitationAsync(stack, "revoked@example.com", i => i.Revoke(stack.OwnerId, BaseTime.AddHours(1)));
        var expired = await SeedInvitationAsync(stack, "expired@example.com", i => i.Expire(BaseTime.AddDays(8)));
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);

        var invitations = await ListInvitationsAsync(owner, stack.WorkspaceId);

        invitations.Select(i => (i.Id, i.Status, i.IsAccepted)).Should().BeEquivalentTo(new[]
        {
            (pending, "Pending", false),
            (revoked, "Revoked", false),
            (expired, "Expired", false),
        });
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

    private static async Task<WorkspaceDto> GetWorkspaceAsync(ServiceProvider provider, Guid workspaceId) =>
        ((Result<WorkspaceDto>)(await SendAsync(provider, new GetWorkspaceQuery(workspaceId)))!).Data!;

    private static async Task<List<WorkspaceDto>> ListUserWorkspacesAsync(ServiceProvider provider) =>
        ((Result<List<WorkspaceDto>>)(await SendAsync(provider, new GetUserWorkspacesQuery()))!).Data!;

    private static async Task<CursorPage<WorkspaceMemberDto>> ListMembersPageAsync(ServiceProvider provider, Guid workspaceId, string? cursor = null, int? limit = null) =>
        ((Result<CursorPage<WorkspaceMemberDto>>)(await SendAsync(provider, new GetWorkspaceMembersQuery(workspaceId, cursor, limit)))!).Data!;

    private static async Task<WorkspaceMemberDto[]> ListMembersAsync(ServiceProvider provider, Guid workspaceId) =>
        (await ListMembersPageAsync(provider, workspaceId)).Items;

    private static async Task<List<WorkspaceInvitationDto>> ListInvitationsAsync(ServiceProvider provider, Guid workspaceId) =>
        ((Result<List<WorkspaceInvitationDto>>)(await SendAsync(provider, new GetWorkspaceInvitationsQuery(workspaceId)))!).Data!;

    private static async Task<WorkspaceMemberDto> GetMyMembershipAsync(ServiceProvider provider, Guid workspaceId) =>
        ((Result<WorkspaceMemberDto>)(await SendAsync(provider, new GetMyWorkspaceMembershipQuery(workspaceId)))!).Data!;

    private async Task<long> PersistedMemberVersionAsync(Guid workspaceId, Guid userId)
    {
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        return await verify.WorkspaceMembers.Where(m => m.WorkspaceId == workspaceId && m.UserId == userId).Select(m => m.Version).SingleAsync();
    }

    private async Task<long> PersistedVersionAsync(Guid workspaceId)
    {
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        return await verify.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Version).SingleAsync();
    }

    private async Task<WorkspaceStatus> PersistedStatusAsync(Guid workspaceId)
    {
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        return await verify.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Status).SingleAsync();
    }

    private async Task<Guid> AddMemberAsync(
        (Guid AccountId, Guid OwnerId, Guid MemberId, Guid WorkspaceId) stack,
        Action<WorkspaceMember> transition)
    {
        var userId = await SeedUserAsync();
        var member = WorkspaceMember.Create(stack.AccountId, stack.WorkspaceId, userId, WorkspaceRole.Member, stack.OwnerId, BaseTime);
        transition(member);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.AccountMembers.Add(AccountMember.Create(stack.AccountId, userId, AccountRole.Member, stack.OwnerId, BaseTime));
        seed.WorkspaceMembers.Add(member);
        await seed.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> SeedInvitationAsync(
        (Guid AccountId, Guid OwnerId, Guid MemberId, Guid WorkspaceId) stack,
        string email,
        Action<WorkspaceInvitation> transition)
    {
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var invitation = WorkspaceInvitation.Create(
            stack.AccountId,
            stack.WorkspaceId,
            email,
            WorkspaceRole.Member,
            InvitationTokenHash.Create(issued.TokenHash),
            issued.HashVersion,
            stack.OwnerId,
            BaseTime);
        transition(invitation);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.WorkspaceInvitations.Add(invitation);
        await seed.SaveChangesAsync();
        return invitation.Id;
    }

    private async Task<Guid> SeedUserAsync()
    {
        var user = User.Create($"wx-adm-{Guid.NewGuid():N}@example.com", "WX Person", "hashed", BaseTime, true);
        user.ConfirmEmail(user.Id, BaseTime);
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        return user.Id;
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid MemberId, Guid WorkspaceId)> SeedWorkspaceStackAsync()
    {
        var now = BaseTime;
        var owner = User.Create($"wx-adm-{Guid.NewGuid():N}@example.com", "WX Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"wx-adm-{Guid.NewGuid():N}@example.com", "WX Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("WX Admin Account", $"wx-adm-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "WX Admin Workspace", $"wx-adm-ws-{Guid.NewGuid():N}", now);

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
            RequestDescriptorRegistry.Create(typeof(GetWorkspaceQuery).Assembly));

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
        services.AddScoped<IMemberPhoneLookup, Notrelix.Infrastructure.Identity.Profiles.MemberPhoneLookup>();
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

        services.AddScoped<IRequestHandler<GetWorkspaceQuery, Result<WorkspaceDto>>, GetWorkspaceQueryHandler>();
        services.AddScoped<IRequestHandler<GetUserWorkspacesQuery, Result<List<WorkspaceDto>>>, GetUserWorkspacesQueryHandler>();
        services.AddScoped<IRequestHandler<GetWorkspaceMembersQuery, Result<CursorPage<WorkspaceMemberDto>>>, GetWorkspaceMembersQueryHandler>();
        services.AddScoped<IRequestHandler<GetMyWorkspaceMembershipQuery, Result<WorkspaceMemberDto>>, GetMyWorkspaceMembershipQueryHandler>();
        services.AddScoped<IRequestHandler<GetWorkspaceInvitationsQuery, Result<List<WorkspaceInvitationDto>>>, GetWorkspaceInvitationsQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateWorkspaceProfileCommand, Result>, UpdateWorkspaceProfileCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateWorkspaceSettingsCommand, Result>, UpdateWorkspaceSettingsCommandHandler>();
        services.AddScoped<IRequestHandler<ArchiveWorkspaceCommand, Result>, ArchiveWorkspaceCommandHandler>();

        return services.BuildServiceProvider();
    }
}
