using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Application.EventMappers.Workspaces;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// TAC-WG-001 / TAC-WG-002 / BOUND-TX-002 production-graph evidence: the full
/// AcceptInvitation handler graph executes inside the caller's request
/// transaction, so the Account-side mutation (IAccountMembershipActions) and
/// the Workspace-side mutation (member + grant + invitation acceptance)
/// commit atomically or roll back together. The already-member path completes
/// the invitation without emitting a second "workspace.member.added" event.
/// </summary>
[Collection("Database")]
public sealed class AcceptInvitationTransactionEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedTime = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AcceptInvitationTransactionEvidenceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed record InvitationGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid UserId,
        string RawToken,
        Guid InvitationId);

    private async Task<InvitationGraph> SeedInvitationAsync(Guid? workspaceId = null)
    {
        var accountId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var slug = $"accept-{Guid.CreateVersion7():N}";
        var account = Account.Create("Accept Evidence Account", $"accept-{Guid.CreateVersion7():N}", AccountType.Team, ownerId, FixedTime);
        var user = User.Create("accept@example.com", "Accept User", "hashed", FixedTime, true);
        user.ConfirmEmail(user.Id, FixedTime);
        var workspace = Workspace.Create(account.Id, ownerId, "Accept Evidence WS", slug, FixedTime);
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var invitation = WorkspaceInvitation.Create(
            account.Id,
            workspace.Id,
            "accept@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create(issued.TokenHash),
            issued.HashVersion,
            ownerId,
            FixedTime.AddDays(-1));

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Accounts.Add(account);
        seed.Users.Add(user);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceInvitations.Add(invitation);
        await seed.SaveChangesAsync();

        return new InvitationGraph(account.Id, workspace.Id, user.Id, issued.RawToken, invitation.Id);
    }

    private (
        EfRequestDataSession Session,
        ApplicationDbContext Context,
        AcceptInvitationCommandHandler Handler,
        Mock<ICurrentRequestContext> RequestContext) Create(Guid userId)
    {
        var context = _db.CreateContext(SystemTenant());
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), SystemTenant()),
            NullLogger<EfRequestDataSession>.Instance);

        var grantProjection = new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));
        var accountGrantProjection = new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(userId);
        requestContext.Setup(r => r.IsAuthenticated).Returns(true);

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(d => d.UtcNow).Returns(FixedTime);

        var handler = new AcceptInvitationCommandHandler(
            context,
            new IdentityUserFactsProvider(context),
            new AccountMembershipActions(context, accountGrantProjection),
            new AccountMembershipFactsProvider(context),
            new OneTimeTokenService(),
            requestContext.Object,
            dateTime.Object,
            grantProjection);

        return (session, context, handler, requestContext);
    }

    private async Task<Result<AcceptInvitationResultDto>> RunInTransactionAsync(
        EfRequestDataSession session,
        AcceptInvitationCommandHandler handler,
        string rawToken,
        Func<Task>? afterHandler = null)
    {
        return await session.ExecuteAsync(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var result = await handler.Handle(new AcceptInvitationCommand(rawToken), ct);
                if (afterHandler is not null)
                    await afterHandler();
                return result;
            },
            CancellationToken.None);
    }

    [Fact]
    public async Task AcceptInvitation_InsideTransactionalSession_CommitsAccountAndWorkspaceTogether()
    {
        var graph = await SeedInvitationAsync();
        var (session, context, handler, _) = Create(graph.UserId);

        var result = await RunInTransactionAsync(session, handler, graph.RawToken);

        result.Succeeded.Should().BeTrue();
        result.Data!.WorkspaceId.Should().Be(graph.WorkspaceId);
        result.Data.WorkspaceSlug.Should().Be(await WorkspaceSlugAsync(graph.WorkspaceId));

        context.ChangeTracker.Clear();
        var member = await context.WorkspaceMembers
            .SingleAsync(m => m.WorkspaceId == graph.WorkspaceId && m.UserId == graph.UserId);
        member.Should().NotBeNull();

        var accountMember = await context.AccountMembers
            .SingleAsync(m => m.AccountId == graph.AccountId && m.UserId == graph.UserId);
        accountMember.Status.Should().Be(AccountMemberStatus.Active);

        var grant = await context.AccessGrants
            .SingleAsync(g => g.WorkspaceId == graph.WorkspaceId && g.UserId == graph.UserId);
        grant.SourceContext.Should().Be("Workspace");
        grant.MembershipStatus.Should().Be("Active");

        var invitation = await context.WorkspaceInvitations
            .SingleAsync(i => i.Id == graph.InvitationId);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
    }

    [Fact]
    public async Task AcceptInvitation_WhenWorkspaceSideFails_RollsBackEntireGraph()
    {
        var graph = await SeedInvitationAsync();
        var (session, context, handler, _) = Create(graph.UserId);

        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var result = await handler.Handle(new AcceptInvitationCommand(graph.RawToken), ct);
result.Succeeded.Should().BeTrue();
                throw new InvalidOperationException("workspace-side failure after handler");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        context.ChangeTracker.Clear();
        (await context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == graph.WorkspaceId && m.UserId == graph.UserId))
            .Should().BeNull("the workspace member must roll back with the request transaction");
        (await context.AccountMembers
            .FirstOrDefaultAsync(m => m.AccountId == graph.AccountId && m.UserId == graph.UserId))
            .Should().BeNull("the account membership must roll back with the request transaction");
        (await context.AccessGrants
            .FirstOrDefaultAsync(g => g.WorkspaceId == graph.WorkspaceId && g.UserId == graph.UserId))
            .Should().BeNull("the grant projection must roll back with the request transaction");
        (await context.WorkspaceInvitations.SingleAsync(i => i.Id == graph.InvitationId)).Status
            .Should().Be(WorkspaceInvitationStatus.Pending, "the invitation acceptance must roll back");
    }

    [Fact]
    public async Task AlreadyMember_AcceptsInvitation_EmitsNoSecondMembershipEvent()
    {
        var graph = await SeedInvitationAsync();

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(
                graph.AccountId, graph.WorkspaceId, graph.UserId, WorkspaceRole.Member, graph.UserId, FixedTime));
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var grantProjection = new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));
        var accountGrantProjection = new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));
        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(graph.UserId);
        requestContext.Setup(r => r.IsAuthenticated).Returns(true);
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(d => d.UtcNow).Returns(FixedTime);
        var handler = new AcceptInvitationCommandHandler(
            context,
            new IdentityUserFactsProvider(context),
            new AccountMembershipActions(context, accountGrantProjection),
            new AccountMembershipFactsProvider(context),
            new OneTimeTokenService(),
            requestContext.Object,
            dateTime.Object,
            grantProjection);

        var result = await handler.Handle(new AcceptInvitationCommand(graph.RawToken), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        (await context.WorkspaceInvitations.SingleAsync(i => i.Id == graph.InvitationId)).Status
            .Should().Be(WorkspaceInvitationStatus.Accepted);

        using (var probe = _db.CreateContext(SystemTenant()))
        {
            var membershipEvents = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .CountAsync(m => m.WorkspaceId == graph.WorkspaceId
                    && m.MessageName == "workspace.member.added");
            membershipEvents.Should().Be(0,
                "an already-member acceptance is a membership no-op: it must not emit a second workspace.member.added event");
        }
    }

    private static DomainEventInterceptor CreateOutboxInterceptor()
    {
        return new DomainEventInterceptor(
            new FixedClock(FixedTime),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(
                new ServiceCollection()
                    .AddScoped<IIntegrationEventMapper, WorkspaceEventMapper>()
                    .BuildServiceProvider()),
            new IntegrationEventCollector());
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private async Task<string> WorkspaceSlugAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Workspaces.Where(w => w.Id == workspaceId).Select(w => w.Slug).SingleAsync();
    }
}