using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("email_outbox", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DeduplicationKey).IsRequired().HasMaxLength(320);
        builder.HasIndex(x => x.DeduplicationKey).IsUnique();

        builder.Property(x => x.SourceContext).HasMaxLength(80);
        builder.Property(x => x.SourceEventId);
        builder.Property(x => x.SourceMessageId);
        builder.Property(x => x.WorkspaceId);
        builder.Property(x => x.RecipientUserId);
        builder.Property(x => x.RecipientEmail).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(240);
        builder.Property(x => x.TemplateName).IsRequired().HasMaxLength(160);
        builder.Property(x => x.TemplateVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(320);
        builder.Property(x => x.TemplateDataJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.HeadersJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(100);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(40).HasDefaultValue("Pending");
        builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxRetries).IsRequired().HasDefaultValue(5);
        builder.Property(x => x.NextAttemptAt).IsRequired();
        builder.Property(x => x.LockedBy).HasMaxLength(160);
        builder.Property(x => x.Provider).HasMaxLength(120);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(240);
        builder.Property(x => x.LastErrorCode).HasMaxLength(120);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.Status, x.Priority, x.NextAttemptAt, x.CreatedAt })
            .HasFilter("\"status\" IN ('Pending', 'Failed')");
        builder.HasIndex(x => new { x.Status, x.LockedUntil, x.ProcessingStartedAt })
            .HasFilter("\"status\" = 'Sending'");
        builder.HasIndex(x => new { x.WorkspaceId, x.CreatedAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.RecipientUserId, x.CreatedAt })
            .HasFilter("\"recipient_user_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => x.SourceMessageId)
            .HasFilter("\"source_message_id\" IS NOT NULL");
    }
}
