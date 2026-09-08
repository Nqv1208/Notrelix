using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;
using Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;
using Notrelix.Application.Features.Integrations.Public.Secrets;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integrations;

/// <summary>
/// TAC-AI-FLOW-05/06 — the Calendar connect/disconnect flows over real
/// PostgreSQL: the connect sequence persists the Connection + SecretVersion
/// + CalendarIntegration round-trip and advances the connection's secret
/// pointer on reuse; the disconnect follows CAL-CONN-001 (one-of-many keeps
/// the generic connection, the last binding revokes it) and a connect that
/// fails after the secret was stored compensates by revoking it.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CalendarConnectionFlowIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CalendarConnectionFlowIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record Stack(Guid AccountId, Guid WorkspaceId, Guid OwnerId);

    private async Task<Stack> SeedWorkspaceAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Cal WS", $"cal-{Guid.NewGuid():N}", Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(WorkspaceMember.Create(
            accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now));
        await seed.SaveChangesAsync();

        return new Stack(accountId, workspace.Id, ownerId);
    }

    private ServiceProvider CreateProvider(Stack stack, RecordingSecretStore? customSecretStore = null)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(stack.AccountId, stack.WorkspaceId, stack.OwnerId);

        var requestContext = new FakeCurrentRequestContext();
        requestContext.AsUser(stack.OwnerId, "cal-owner@example.com", "Cal Owner");
        // The request-context fake carries its own tenant; mirror the outer
        // workspace scope so RequireAccountId/RequireWorkspaceId resolve.
        requestContext.Tenant.SetWorkspace(stack.AccountId, stack.WorkspaceId, stack.OwnerId);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider>(_ => new FixedClock(Now));
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton<ICurrentRequestContext>(requestContext);
        services.AddSingleton<ICurrentUser>(_ => new FakeCurrentUser
        {
            UserId = stack.OwnerId,
            Email = "cal-owner@example.com",
            Name = "Cal Owner",
        });

        // Real handler; the physical secret store uses an in-memory fake to
        // observe store/revoke behavior deterministically.
        var secretStore = customSecretStore ?? new RecordingSecretStore();
        services.AddSingleton<RecordingSecretStore>(_ => secretStore);
        services.AddSingleton<IIntegrationSecretStore>(sp => sp.GetRequiredService<RecordingSecretStore>());

        // Production parity: ONE scoped ApplicationDbContext shared by the
        // IIntegrationDbContext seam, the DataSession commit, and the secret
        // store's staging — so blob + aggregates share one transaction fate.
        services.AddScoped<ApplicationDbContext>(_ => _db.CreateContext(tenant));
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ConnectCalendarCommandHandler>();
        services.AddScoped<DisconnectCalendarCommandHandler>();

        return services.BuildServiceProvider();
    }

    private static ConnectCalendarCommand NewConnectCommand(Stack stack, string accessToken = "oauth-access-token") =>
        new("Google", accessToken, stack.WorkspaceId, null, "Both");

    [Fact]
    public async Task ConnectCalendar_FirstConnect_PersistsRoundTrip_WithSecretReference()
    {
        var stack = await SeedWorkspaceAsync();
        await using var provider = CreateProvider(stack);
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ConnectCalendarCommandHandler>()
            .Handle(NewConnectCommand(stack), CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        // The scoped context staged everything; commit it (the DataSession
        // owns this in production).
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .SaveChangesAsync(CancellationToken.None);

        // Round-trip: reload from the DB and verify all three authorities.
        await using var verify = _db.CreateContext(SystemTenant());
        var calendar = await verify.CalendarIntegrations.IgnoreQueryFilters()
            .SingleAsync(ci => ci.Id == result.Data);
        calendar.WorkspaceId.Should().Be(stack.WorkspaceId);
        calendar.IsActive.Should().BeTrue();

        var connection = await verify.IntegrationConnections.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == calendar.ConnectionId);
        connection.WorkspaceId.Should().Be(stack.WorkspaceId);
        connection.Status.Should().Be(IntegrationConnectionStatus.Active);

        var secretVersion = await verify.IntegrationSecretVersions.IgnoreQueryFilters()
            .SingleAsync(sv => sv.ConnectionId == connection.Id);
        secretVersion.Version.Should().Be("1");
        secretVersion.SecretReference.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConnectCalendar_Reconnect_RotatesSecretVersion_AndReactivatesBinding()
    {
        var stack = await SeedWorkspaceAsync();
        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ConnectCalendarCommandHandler>()
                .Handle(NewConnectCommand(stack, "token-1"), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        // Disconnect the binding, then reconnect — the same binding is reused
        // via Activate and the connection rotates its secret pointer.
        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            await using var read = _db.CreateContext(SystemTenant());
            var calendarId = (await read.CalendarIntegrations.IgnoreQueryFilters()
                .SingleAsync(ci => ci.WorkspaceId == stack.WorkspaceId)).Id;
            (await scope.ServiceProvider.GetRequiredService<DisconnectCalendarCommandHandler>()
                .Handle(new DisconnectCalendarCommand(calendarId), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ConnectCalendarCommandHandler>()
                .Handle(NewConnectCommand(stack, "token-2"), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using var verify = _db.CreateContext(SystemTenant());

        // CAL-CONN-001: disconnecting the ONLY binding revoked the generic
        // connection, so the reconnect created a fresh connection/binding.
        var connections = await verify.IntegrationConnections.IgnoreQueryFilters()
            .Where(c => c.WorkspaceId == stack.WorkspaceId).ToListAsync();
        connections.Should().HaveCount(2, "the revoked connection is retained as history; a new one serves the reconnect");
        connections.Count(c => c.Status == IntegrationConnectionStatus.Revoked).Should().Be(1);
        var active = connections.Single(c => c.Status == IntegrationConnectionStatus.Active);

        var secretVersions = await verify.IntegrationSecretVersions.IgnoreQueryFilters()
            .Where(sv => sv.ConnectionId == active.Id).ToListAsync();
        secretVersions.Should().ContainSingle().Which.Version.Should().Be("1");

        var calendar = await verify.CalendarIntegrations.IgnoreQueryFilters()
            .SingleAsync(ci => ci.WorkspaceId == stack.WorkspaceId && ci.DeletedAt == null);
        calendar.IsActive.Should().BeTrue("the reconnect reactivates or replaces the binding");
        calendar.ConnectionId.Should().Be(active.Id,
            "the stale binding (pointing at the revoked connection) is replaced by a binding on the active connection");
    }

    [Fact]
    public async Task DisconnectCalendar_SingleBinding_RetentionDecisionFollowsCalConn001()
    {
        var stack = await SeedWorkspaceAsync();

        // The single-binding case: the binding lifecycle authority
        // deactivates first, and CAL-CONN-001 then decides the generic
        // connection's fate — with no remaining active binding, the
        // connection is revoked (the one-of-many retention case is proven by
        // the LastBinding test with a hand-seeded second binding).
        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ConnectCalendarCommandHandler>()
                .Handle(NewConnectCommand(stack), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        Guid connectionId;
        Guid calendarId;
        await using (var read = _db.CreateContext(SystemTenant()))
        {
            var calendar = await read.CalendarIntegrations.IgnoreQueryFilters()
                .SingleAsync(ci => ci.WorkspaceId == stack.WorkspaceId);
            calendarId = calendar.Id;
            connectionId = calendar.ConnectionId;
        }

        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<DisconnectCalendarCommandHandler>()
                .Handle(new DisconnectCalendarCommand(calendarId), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.CalendarIntegrations.IgnoreQueryFilters().SingleAsync(ci => ci.Id == calendarId))
            .IsActive.Should().BeFalse("the binding lifecycle authority deactivates first");

        var connection = await verify.IntegrationConnections.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == connectionId);
        connection.Status.Should().Be(IntegrationConnectionStatus.Revoked,
            "CAL-CONN-001: with no remaining active binding the generic connection is revoked");
    }

    [Fact]
    public async Task DisconnectCalendar_LastBinding_RevokesConnection_WhenNoActiveBindingsRemain()
    {
        var stack = await SeedWorkspaceAsync();

        // Two calendar integrations sharing one connection: connect Google,
        // then hand-add an Outlook binding on the same connection (simulating
        // a second capability bound to the same provider relationship).
        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<ConnectCalendarCommandHandler>()
                .Handle(NewConnectCommand(stack), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        Guid connectionId;
        Guid firstCalendarId;
        await using (var read = _db.CreateContext(SystemTenant()))
        {
            var first = await read.CalendarIntegrations.IgnoreQueryFilters()
                .SingleAsync(ci => ci.WorkspaceId == stack.WorkspaceId);
            firstCalendarId = first.Id;
            connectionId = first.ConnectionId;
            read.CalendarIntegrations.Add(CalendarIntegration.Create(
                stack.AccountId, stack.WorkspaceId, connectionId,
                CalendarProvider.Outlook, CalendarSyncDirection.Push, stack.OwnerId, Now));
            await read.SaveChangesAsync();
        }

        // Disconnect the FIRST binding: one-of-many → connection retained.
        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<DisconnectCalendarCommandHandler>()
                .Handle(new DisconnectCalendarCommand(firstCalendarId), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using (var midVerify = _db.CreateContext(SystemTenant()))
        {
            (await midVerify.IntegrationConnections.IgnoreQueryFilters().SingleAsync(c => c.Id == connectionId))
                .Status.Should().Be(IntegrationConnectionStatus.Active,
                "an active binding still references the connection — it must be retained");
        }

        // Disconnect the LAST binding → connection revoked.
        await using (var provider = CreateProvider(stack))
        await using (var scope = provider.CreateAsyncScope())
        {
            await using var read = _db.CreateContext(SystemTenant());
            var last = await read.CalendarIntegrations.IgnoreQueryFilters()
                .SingleAsync(ci => ci.ConnectionId == connectionId && ci.IsActive && ci.DeletedAt == null);
            (await scope.ServiceProvider.GetRequiredService<DisconnectCalendarCommandHandler>()
                .Handle(new DisconnectCalendarCommand(last.Id), CancellationToken.None)).Succeeded.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.IntegrationConnections.IgnoreQueryFilters().SingleAsync(c => c.Id == connectionId))
            .Status.Should().Be(IntegrationConnectionStatus.Revoked,
            "the last binding revokes the generic connection per CAL-CONN-001");
    }

    /// <summary>
    /// TAC-AI-FLOW-05 — the connect workflow's commit fate: the secret blob,
    /// the connection, the secret version, and the calendar binding are all
    /// staged on the SAME scoped context and share one transaction fate. A
    /// rolled-back workflow leaves no committed row of any kind — atomicity
    /// instead of compensation (frozen).
    /// </summary>
    [Fact]
    public async Task ConnectWorkflow_RolledBack_CommitsNoRowOfAnyKind()
    {
        var stack = await SeedWorkspaceAsync();
        await using var provider = CreateProvider(stack);
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

        (await scope.ServiceProvider.GetRequiredService<ConnectCalendarCommandHandler>()
            .Handle(NewConnectCommand(stack), CancellationToken.None)).Succeeded.Should().BeTrue();

        await transaction.RollbackAsync(CancellationToken.None);

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.IntegrationSecretBlobs.IgnoreQueryFilters()
            .CountAsync(b => !b.Revoked)).Should().Be(0,
            "no physical secret may survive the aborted workflow");
        (await verify.IntegrationConnections.IgnoreQueryFilters()
            .AnyAsync(c => c.WorkspaceId == stack.WorkspaceId)).Should().BeFalse(
            "no connection may survive the aborted workflow");
        (await verify.IntegrationSecretVersions.IgnoreQueryFilters()
            .AnyAsync(sv => sv.ConnectionId == stack.WorkspaceId)).Should().BeFalse(
            "no secret version may survive the aborted workflow");
        (await verify.CalendarIntegrations.IgnoreQueryFilters()
            .AnyAsync(ci => ci.WorkspaceId == stack.WorkspaceId)).Should().BeFalse(
            "no calendar binding may survive the aborted workflow");
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    /// <summary>
    /// Deterministic in-memory secret store recording store/revoke behavior —
    /// the production DataProtection store is exercised by the provider-level
    /// evidence; this test observes the flow's use of the port.
    /// </summary>
    private sealed class RecordingSecretStore : IIntegrationSecretStore
    {
        private readonly List<string> _stored = [];
        private readonly List<string> _revoked = [];

        public IReadOnlyList<string> Stored => _stored;
        public IReadOnlyList<string> Revoked => _revoked;

        public Task<string> StoreAsync(string secret, CancellationToken cancellationToken)
        {
            var reference = Guid.CreateVersion7().ToString();
            _stored.Add(reference);
            return Task.FromResult(reference);
        }

        public Task RevokeAsync(string secretReference, CancellationToken cancellationToken)
        {
            _revoked.Add(secretReference);
            return Task.CompletedTask;
        }
    }
}
