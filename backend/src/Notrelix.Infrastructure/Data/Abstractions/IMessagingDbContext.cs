using Notrelix.Infrastructure.Data.Messaging;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Infrastructure.Data.Abstractions;

public interface IMessagingDbContext
{
    DbSet<MessagingOutboxMessage> MessagingOutboxMessages { get; }
    DbSet<OutboxDeliveryAttempt> OutboxDeliveryAttempts { get; }
    DbSet<MessagingProcessedEvent> MessagingProcessedEvents { get; }
}