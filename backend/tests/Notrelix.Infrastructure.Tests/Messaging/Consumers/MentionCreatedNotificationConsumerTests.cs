using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Common.Context;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Infrastructure.Data.Projections.Search;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging.Consumers;

/// <summary>
/// UC-COL-H06: a redelivered mention-created event produces one notification per (mention, person). The read before
/// the insert is only a fast path; a concurrent delivery that wins the insert makes this one a no-op, not an error.
/// </summary>
public sealed class MentionCreatedNotificationConsumerTests
{
    private const string DedupIndex = "ux_notifications_items_dedup";

    [Fact]
    public async Task FirstDelivery_CreatesOneNotificationAndOneRecipient()
    {
        var inner = CreateContext();
        var consumer = new MentionCreatedNotificationConsumer(inner, NullLogger<MentionCreatedNotificationConsumer>.Instance);

        await consumer.Consume(ConsumeFor(Message()));

        inner.NotificationItems.Should().ContainSingle();
        inner.NotificationRecipients.Should().ContainSingle();
    }

    [Fact]
    public async Task RedeliveryAfterTheFirstCommit_IsSkippedByTheFastPath()
    {
        var inner = CreateContext();
        var consumer = new MentionCreatedNotificationConsumer(inner, NullLogger<MentionCreatedNotificationConsumer>.Instance);
        var message = Message();
        await consumer.Consume(ConsumeFor(message));

        await consumer.Consume(ConsumeFor(message));

        inner.NotificationItems.Should().ContainSingle();
    }

    [Fact]
    public async Task ConcurrentDeliveryWonTheInsert_ThisDeliveryIsANoOpAndDoesNotThrow()
    {
        // The pre-check finds nothing; the other delivery commits the same deduplication key before this save.
        var inner = CreateContext();
        var racing = new RacingNotificationContext(inner, DeduplicationViolation());
        var consumer = new MentionCreatedNotificationConsumer(racing, NullLogger<MentionCreatedNotificationConsumer>.Instance);

        var act = () => consumer.Consume(ConsumeFor(Message()));

        await act.Should().NotThrowAsync("a duplicate delivery is a success (UC-COL-H06), not a retry");
        racing.SaveAttempts.Should().Be(1);
        inner.NotificationItems.Should().BeEmpty("the losing delivery must not store a second notification");
    }

    [Fact]
    public async Task AnyOtherUniqueViolation_IsNotSwallowed()
    {
        var inner = CreateContext();
        var racing = new RacingNotificationContext(inner, new DbUpdateException(
            "unique",
            new Exception("23505: duplicate key value violates unique constraint \"ux_notification_recipients_other\"")));
        var consumer = new MentionCreatedNotificationConsumer(racing, NullLogger<MentionCreatedNotificationConsumer>.Instance);

        await consumer.Invoking(c => c.Consume(ConsumeFor(Message())))
            .Should().ThrowAsync<DbUpdateException>("only the deduplication key makes a delivery a no-op");
    }

    private static DbUpdateException DeduplicationViolation() =>
        new("An error occurred while saving the entity changes.",
            new Exception($"23505: duplicate key value violates unique constraint \"{DedupIndex}\""));

    private static MentionCreatedIntegrationEvent Message() => new(
        EventId: Guid.CreateVersion7(),
        AccountId: Guid.CreateVersion7(),
        MentionId: Guid.CreateVersion7(),
        WorkspaceId: Guid.CreateVersion7(),
        TargetType: "collaboration.comment",
        TargetId: Guid.CreateVersion7(),
        MentionedUserId: Guid.CreateVersion7(),
        MentionedByUserId: Guid.CreateVersion7(),
        CorrelationId: Guid.CreateVersion7(),
        Excerpt: "hi");

    private static ConsumeContext<MentionCreatedIntegrationEvent> ConsumeFor(MentionCreatedIntegrationEvent message)
    {
        var context = new Mock<ConsumeContext<MentionCreatedIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return new MentionTestContext(options, tenant);
    }

    private sealed class MentionTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }

    /// <summary>Delegates reads and adds to the real context, and fails the save the way a concurrent winner does.</summary>
    private sealed class RacingNotificationContext(INotificationDbContext inner, Exception saveFailure) : INotificationDbContext
    {
        public int SaveAttempts { get; private set; }

        public DbSet<NotificationItemRecord> NotificationItems => inner.NotificationItems;
        public DbSet<NotificationRecipientRecord> NotificationRecipients => inner.NotificationRecipients;
        public DbSet<Notrelix.Domain.Collaboration.Preferences.NotificationPreferenceSet> NotificationPreferenceSets => inner.NotificationPreferenceSets;
        public DbSet<NotificationCounterRecord> NotificationCounters => inner.NotificationCounters;
        public DbSet<NotificationThreadMuteRecord> NotificationThreadMutes => inner.NotificationThreadMutes;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveAttempts++;
            throw saveFailure;
        }
    }
}
