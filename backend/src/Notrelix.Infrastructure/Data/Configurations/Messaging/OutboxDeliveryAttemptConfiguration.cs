using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Messaging;

namespace Notrelix.Infrastructure.Data.Configurations.Messaging;

public sealed class OutboxDeliveryAttemptConfiguration : IEntityTypeConfiguration<OutboxDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<OutboxDeliveryAttempt> builder)
    {
        builder.ToTable("outbox_delivery_attempts", DbSchemas.Messaging);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.OutboxMessageId).IsRequired();
        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.AttemptNo).IsRequired();
        builder.Property(x => x.DispatcherId).HasMaxLength(160);
        builder.Property(x => x.Broker).HasMaxLength(120);
        builder.Property(x => x.Destination).HasMaxLength(240);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(40);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.DurationMs);
        builder.Property(x => x.ErrorCode).HasMaxLength(120);
        builder.Property(x => x.ErrorDetailJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(x => new { x.OutboxMessageId, x.AttemptNo }).IsUnique();
        builder.HasIndex(x => new { x.EventId, x.StartedAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.Status, x.StartedAt }).IsDescending(false, true);
    }
}
