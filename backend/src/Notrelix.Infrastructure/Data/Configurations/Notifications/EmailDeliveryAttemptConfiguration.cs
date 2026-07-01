using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class EmailDeliveryAttemptConfiguration : IEntityTypeConfiguration<EmailDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryAttempt> builder)
    {
        builder.ToTable("email_delivery_attempts", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EmailOutboxId).IsRequired();
        builder.Property(x => x.AttemptNo).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(120);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(240);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(40);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.DurationMs);
        builder.Property(x => x.ErrorCode).HasMaxLength(120);
        builder.Property(x => x.ProviderResponseJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(x => new { x.EmailOutboxId, x.AttemptNo }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.StartedAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.Provider, x.ProviderMessageId })
            .HasFilter("\"provider\" IS NOT NULL AND \"provider_message_id\" IS NOT NULL");
    }
}
