using Npgsql;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

[Collection("Database")]
public class CreateWorkspaceCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private readonly Mock<ICurrentRequestContext> _requestContextMock;
    private readonly Mock<IDateTimeProvider> _dateTimeMock;

    public CreateWorkspaceCommandHandlerTests(PostgresTestContainer db)
    {
        _db = db;
        _requestContextMock = new Mock<ICurrentRequestContext>();
        _requestContextMock.Setup(r => r.RequireAccountId()).Returns(Guid.NewGuid());
        _requestContextMock.Setup(r => r.UserId).Returns(Guid.NewGuid());
        _dateTimeMock = new Mock<IDateTimeProvider>();
        _dateTimeMock.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
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

    [Fact]
    public async Task Handle_WhenCreatingTeamWorkspace_ShouldSucceed()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var userId = Guid.NewGuid();
        _requestContextMock.Setup(r => r.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("Awesome Project", "A great software project", false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        await context.SaveChangesAsync();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("Awesome Project");
        workspace.Slug.Should().Be("awesome-project");
        workspace.Description.Should().Be("A great software project");
        workspace.IsPersonal.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCreatingPersonalWorkspace_ShouldSucceed()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var userId = Guid.NewGuid();
        _requestContextMock.Setup(r => r.UserId).Returns(userId);

        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("My Personal Tasks", null, true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        await context.SaveChangesAsync();

        var workspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        workspace.Should().NotBeNull();
        workspace!.Name.Should().Be("My Personal Tasks");
        workspace.IsPersonal.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldAppendUniqueSuffix()
    {
        await using var context = _db.CreateContext(SystemTenant());
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        _requestContextMock.Setup(r => r.UserId).Returns(userId);
        _dateTimeMock.Setup(d => d.UtcNow).Returns(now);

        var existingWorkspace = Workspace.Create(accountId, userId, "Awesome Project", "awesome-project", now);
        context.Workspaces.Add(existingWorkspace);
        await context.SaveChangesAsync();

        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("Awesome Project", "A duplicate project name", false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().NotBe(existingWorkspace.Id);

        await context.SaveChangesAsync();

        var duplicateWorkspace = await context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == result.Data);

        duplicateWorkspace.Should().NotBeNull();
        duplicateWorkspace!.Name.Should().Be("Awesome Project");
        duplicateWorkspace.Slug.Should().StartWith("awesome-project-");
        duplicateWorkspace.Slug.Length.Should().BeGreaterThan("awesome-project-".Length);
    }

    [Fact]
    public async Task Handle_WhenOwnersWorkspaceCreated_ShouldPersistOwnerGrantProjection()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        _requestContextMock.Setup(r => r.UserId).Returns(userId);
        _dateTimeMock.Setup(d => d.UtcNow).Returns(now);

        await using var context = _db.CreateContext(SystemTenant());
        var handler = new CreateWorkspaceCommandHandler(context, _requestContextMock.Object, _dateTimeMock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var command = new CreateWorkspaceCommand("Grant Project", null, false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        await context.SaveChangesAsync();

        // The grant projection stages into the same SaveChanges as the workspace +
        // owner member. It is not persisted until the caller commits, proving the
        // projection is transaction-part of workspace creation (NRX-016 evidence
        // for the Workspaces-owned grant row used by RLS helpers).
        var grant = await context.AccessGrants
            .SingleAsync(g => g.WorkspaceId == result.Data && g.UserId == userId);

        grant.AccountId.Should().Be(accountId);
        grant.WorkspaceId.Should().Be(result.Data);
        grant.UserId.Should().Be(userId);
        grant.SourceContext.Should().Be("Workspace");
        grant.MembershipStatus.Should().Be("Active");
        grant.RoleCodes.Should().BeEquivalentTo(["Owner"]);
        grant.IsAccountAdmin.Should().BeFalse();
        grant.IsWorkspaceAdmin.Should().BeTrue(
            "the workspace owner must be projected as a workspace admin for RLS access");
        grant.GrantedAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_WhenSlugRacedAcrossConcurrentRequests_FailsClosedWithSinglePersistedWorkspace()
    {
        // Both requests observe "no existing slug" before the other commits, so
        // both select the unsuffixed base slug. The DB unique index — not the
        // pre-check — must be the authoritative fail-closed guard, rejecting the
        // stale concurrent insert and keeping exactly one workspace per slug.
        var accountId = Guid.NewGuid();
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var contextA = _db.CreateContext(SystemTenant());
        await using var contextB = _db.CreateContext(SystemTenant());
        var handlerA = new CreateWorkspaceCommandHandler(
            contextA, _requestContextMock.Object, _dateTimeMock.Object,
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(contextA)));
        var handlerB = new CreateWorkspaceCommandHandler(
            contextB, _requestContextMock.Object, _dateTimeMock.Object,
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(contextB)));

        _requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        _requestContextMock.Setup(r => r.UserId).Returns(aId);
        var commandA = new CreateWorkspaceCommand("Race Project", null, false);
        var resultA = await handlerA.Handle(commandA, CancellationToken.None);
        resultA.Succeeded.Should().BeTrue();
        resultA.Data.Should().NotBeEmpty();

        _requestContextMock.Setup(r => r.UserId).Returns(bId);
        var commandB = new CreateWorkspaceCommand("Race Project", null, false);
        var resultB = await handlerB.Handle(commandB, CancellationToken.None);
        resultB.Succeeded.Should().BeTrue();
        resultB.Data.Should().NotBeEmpty();

        await contextA.SaveChangesAsync();

        var secondCommit = async () => await contextB.SaveChangesAsync();
        var thrown = await secondCommit.Should().ThrowAsync<DbUpdateException>(
            "the DB unique index must reject the concurrent duplicate slug insert");

        var pg = thrown.Which.InnerException as PostgresException;
        pg.Should().NotBeNull("the rejection must originate from PostgreSQL, not an EF-side save failure");
        pg!.ConstraintName.Should().Be("ux_workspaces_account_slug_active",
            "the duplicate concurrent insert must be rejected by the account-scoped active-slug uniqueness guard");

        await using var verify = _db.CreateContext(SystemTenant());
        var workspaces = await verify.Workspaces
            .Where(w => w.AccountId == accountId && w.Slug == "race-project")
            .ToListAsync();

        workspaces.Should().ContainSingle(
            "exactly one concurrent request may persist a given account-scoped slug");

        // Winner B (committed) = contextA state; A's workspace id won the commit.
        verify.Workspaces.Any(w => w.Id == resultA.Data).Should().BeTrue(
            "the committed workspace is the one that reached SaveChanges first");
    }
}
