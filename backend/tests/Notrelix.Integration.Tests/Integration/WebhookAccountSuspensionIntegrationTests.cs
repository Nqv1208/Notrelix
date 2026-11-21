using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Accounts.Public.Status;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Infrastructure.Accounts.Status;
using Notrelix.Infrastructure.Messaging.Consumers.Integrations;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// Outgoing webhooks of a suspended or closed Account are skipped and recorded with the reason
/// <c>account.suspended</c> (a test delivery included), nothing is sent, and an active Account keeps the delivery
/// rules it had. The status port reads the real Account row: Trialing is Active, Closed and an unknown Account are
/// not operating.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WebhookAccountSuspensionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private Guid _accountId, _workspaceId;

    public WebhookAccountSuspensionIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        var owner = User.Create($"wh-{Guid.NewGuid():N}@example.com", "Owner", "hashed", Now, true);
        var account = Account.Create("WH Account", $"wh-{Guid.NewGuid():N}", AccountType.Team, owner.Id, Now);
        var workspace = Notrelix.Domain.Workspaces.Workspaces.Workspace.Create(account.Id, owner.Id, "WH WS", $"wh-ws-{Guid.NewGuid():N}", Now);
        await using var seed = _db.CreateOwnerContext(System());
        seed.Users.Add(owner);
        seed.Accounts.Add(account);
        seed.Workspaces.Add(workspace);
        await seed.SaveChangesAsync();
        _accountId = account.Id;
        _workspaceId = workspace.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Notrelix.Application.Common.Time.IDateTimeProvider Clock()
    {
        var clock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);
        return clock.Object;
    }

    private static FakeCurrentTenantContext System()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private async Task SetStatusAsync(string status)
    {
        await using var ctx = _db.CreateOwnerContext(System());
        await ctx.Database.ExecuteSqlRawAsync("UPDATE account.accounts SET status = {0} WHERE id = {1}", status, _accountId);
    }

    private async Task<AccountStatusFact> FactAsync(Guid accountId)
    {
        await using var ctx = _db.CreateOwnerContext(System());
        return await new AccountStatusFactsReader(ctx).GetAsync(accountId, CancellationToken.None);
    }

    private async Task<OutboundDeliveryLogEntry> DeliverAsync(string type)
    {
        var deliveryId = Guid.CreateVersion7();
        var message = new IntegrationWebhookDispatchRequestedV1(
            Guid.CreateVersion7(), deliveryId, Guid.CreateVersion7(), type, _accountId, _workspaceId, Now, Guid.CreateVersion7());
        var context = new Moq.Mock<ConsumeContext<IntegrationWebhookDispatchRequestedV1>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await using (var ctx = _db.CreateOwnerContext(System()))
        {
            // The sender and the secret store are never reached on a skip, so a refused delivery proves nothing was sent.
            var consumer = new IntegrationWebhookDispatchRequestedConsumer(
                ctx, null!, null!, Clock(), new AccountStatusFactsReader(ctx),
                NullLogger<IntegrationWebhookDispatchRequestedConsumer>.Instance);
            await consumer.Consume(context.Object);
        }

        await using var read = _db.CreateOwnerContext(System());
        return await read.OutboundDeliveryLog.AsNoTracking().SingleAsync(d => d.Id == deliveryId);
    }

    [Theory]
    [InlineData("Active", "ConnectionMissing")]
    [InlineData("Trialing", "ConnectionMissing")]
    [InlineData("Suspended", "AccountSuspended")]
    [InlineData("Closed", "AccountSuspended")]
    public async Task ADelivery_IsSkippedWithTheAccountReason_WhenTheAccountIsSuspendedOrClosed(string status, string reason)
    {
        await SetStatusAsync(status);

        var log = await DeliverAsync("item.created");

        log.Status.Should().Be(OutboundDeliveryStatus.Skipped);
        log.SkipReason.Should().NotBeNull();
        log.SkipReason!.Value.ToString().Should().Be(reason);
        if (reason == "AccountSuspended") log.SkipReason.Value.Code().Should().Be("account.suspended");
    }

    [Fact]
    public async Task ATestDelivery_IsAlsoSkipped_WhileTheAccountIsSuspended()
    {
        await SetStatusAsync("Suspended");

        var log = await DeliverAsync(IntegrationWebhookDispatchRequestedV1.TestType);

        log.SkipReason.Should().Be(OutboundDeliverySkipReason.AccountSuspended);
    }

    [Theory]
    [InlineData("Active", AccountStatusFact.Active)]
    [InlineData("Trialing", AccountStatusFact.Active)]
    [InlineData("Suspended", AccountStatusFact.Suspended)]
    [InlineData("Closed", AccountStatusFact.Closed)]
    public async Task TheStatusPort_ReadsTheAccountRow(string stored, AccountStatusFact expected)
    {
        await SetStatusAsync(stored);

        (await FactAsync(_accountId)).Should().Be(expected);
    }

    [Fact]
    public async Task AnUnknownAccount_IsReportedAsClosed_TheStricterReading()
    {
        (await FactAsync(Guid.CreateVersion7())).Should().Be(AccountStatusFact.Closed);
    }
}
