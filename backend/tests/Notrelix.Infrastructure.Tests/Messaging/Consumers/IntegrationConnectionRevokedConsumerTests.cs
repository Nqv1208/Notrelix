using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Integrations.Public.Secrets;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Infrastructure.Messaging.Consumers.Integrations;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

public sealed class IntegrationConnectionRevokedConsumerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SuccessOutcome_RevokesEveryDistinctSecretReference()
    {
        await using var db = CreateContext();
        var (message, reference) = SeedConnection(db);
        var store = new Mock<IIntegrationSecretStore>();
        store.Setup(s => s.RevokeAsync(reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderCleanupResult(ProviderCleanupOutcome.Success));

        await new IntegrationConnectionRevokedConsumer(
                db,
                store.Object,
                NullLogger<IntegrationConnectionRevokedConsumer>.Instance)
            .Consume(CreateContext(message));

        store.Verify(s => s.RevokeAsync(reference, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ProviderCleanupOutcome.Retryable, typeof(IntegrationSecretCleanupRetryableException))]
    [InlineData(ProviderCleanupOutcome.Unknown, typeof(IntegrationSecretCleanupUnknownException))]
    [InlineData(ProviderCleanupOutcome.Terminal, typeof(IntegrationSecretCleanupTerminalException))]
    public async Task NonSuccessOutcome_IsExplicitlyClassified(
        ProviderCleanupOutcome outcome,
        Type expectedException)
    {
        await using var db = CreateContext();
        var (message, reference) = SeedConnection(db);
        var store = new Mock<IIntegrationSecretStore>();
        store.Setup(s => s.RevokeAsync(reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderCleanupResult(outcome, "test outcome"));

        var action = () => new IntegrationConnectionRevokedConsumer(
                db,
                store.Object,
                NullLogger<IntegrationConnectionRevokedConsumer>.Instance)
            .Consume(CreateContext(message));

        (await action.Should().ThrowAsync<Exception>())
            .Which.GetType().Should().Be(expectedException);
    }

    private static (IntegrationConnectionRevokedIntegrationEvent Message, string Reference) SeedConnection(
        ApplicationDbContext db)
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var connection = IntegrationConnection.Create(
            accountId,
            workspaceId,
            IntegrationProvider.Google,
            actorId,
            Now);
        var reference = Guid.CreateVersion7().ToString();
        connection.RotateSecret("1", SecretRef.Create(reference), actorId, Now);
        db.IntegrationConnections.Add(connection);
        db.IntegrationSecretVersions.Add(
            IntegrationSecretVersion.Create(connection.Id, "1", SecretRef.Create(reference), Now));
        db.SaveChanges();

        return (
            new IntegrationConnectionRevokedIntegrationEvent(
                Guid.CreateVersion7(),
                accountId,
                workspaceId,
                connection.Id,
                Guid.CreateVersion7(),
                actorId,
                null,
                Now),
            reference);
    }

    private static ConsumeContext<IntegrationConnectionRevokedIntegrationEvent> CreateContext(
        IntegrationConnectionRevokedIntegrationEvent message)
    {
        var context = new Mock<ConsumeContext<IntegrationConnectionRevokedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-secret-cleanup-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new TestApplicationDbContext(options);
    }

    private sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
            modelBuilder.Entity<SearchDocumentRecord>().Ignore(x => x.SearchVector);
        }
    }
}
