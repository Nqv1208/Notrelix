using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.EventMappers.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// TAC-WG-007 evidence for the workspace-creation chain (M5.2): a valid
/// Workspace.Create raises the Workspaces-owned domain fact, the producer-owned
/// mapper turns it into the registry identity "workspace.created" V1, and the
/// outbox record is written inside the same SaveChanges. A rolled-back creation
/// leaves no committed outward delivery.
/// </summary>
[Collection("Database")]
public sealed class WorkspaceCreatedOutboxEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkspaceCreatedOutboxEvidenceTests(PostgresTestContainer db)
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

    private static DomainEventInterceptor CreateOutboxInterceptor()
    {
        return new DomainEventInterceptor(
            new FixedClock(Now),
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

    private async Task<int> CountWorkspaceCreatedOutboxAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == "workspace.created");
    }

    private async Task<int> CountMemberAddedOutboxAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == "workspace.member.added");
    }

    [Fact]
    public async Task WorkspaceCreatedFact_OutboxIntent_CommitsAtomically()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var creation = WorkspaceFactory.CreateWithOwner(
            accountId, ownerId, "Created WS", $"created-{Guid.NewGuid():N}", Now);

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        context.Workspaces.Add(creation.Workspace);
        context.WorkspaceMembers.Add(creation.OwnerMember);
        await context.SaveChangesAsync();

        (await CountWorkspaceCreatedOutboxAsync(creation.Workspace.Id)).Should().Be(1,
            "a valid workspace creation must stage exactly one producer-owned outward event");
        (await CountMemberAddedOutboxAsync(creation.Workspace.Id)).Should().Be(1,
            "the owner member is added through the shared WorkspaceMember.Create factory, so the same workspace.member.added event family produced for AcceptInvitation is reused here");
    }

    [Fact]
    public async Task RolledBackWorkspaceCreation_NoCommittedOutwardDelivery()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var creation = WorkspaceFactory.CreateWithOwner(
            accountId, ownerId, "Rolled Back WS", $"rolled-{Guid.NewGuid():N}", Now);

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Workspaces.Add(creation.Workspace);
        context.WorkspaceMembers.Add(creation.OwnerMember);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        (await CountWorkspaceCreatedOutboxAsync(creation.Workspace.Id)).Should().Be(0,
            "a rolled-back workspace creation must leave no committed outward delivery");
        (await CountMemberAddedOutboxAsync(creation.Workspace.Id)).Should().Be(0,
            "a rolled-back workspace creation must leave no committed member-added delivery");
    }
}