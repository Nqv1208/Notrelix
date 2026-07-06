using Notrelix.Infrastructure.Data.Notifications;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Infrastructure.Data.Abstractions;

public interface INotificationDbContext
{
    DbSet<NotificationItemRecord> NotificationItems { get; }
    DbSet<NotificationRecipientRecord> NotificationRecipients { get; }
    DbSet<NotificationPreferenceRecord> CanonicalNotificationPreferences { get; }
    DbSet<NotificationCounterRecord> NotificationCounters { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}