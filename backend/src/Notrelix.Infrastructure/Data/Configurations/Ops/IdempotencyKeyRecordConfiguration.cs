using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public sealed class IdempotencyKeyRecordConfiguration : IEntityTypeConfiguration<IdempotencyKeyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyKeyRecord> builder)
    {
        builder.ToTable("idempotency_keys", DbSchemas.Ops);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Scope).HasColumnName("scope").IsRequired().HasMaxLength(120);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").IsRequired().HasMaxLength(260);
        builder.Property(x => x.RequestMethod).HasColumnName("request_method").IsRequired().HasMaxLength(10);
        builder.Property(x => x.RequestPath).HasColumnName("request_path").IsRequired().HasMaxLength(500);
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(40);
        builder.Property(x => x.ResponseStatusCode).HasColumnName("response_status_code");
        builder.Property(x => x.ResponseBodyJson).HasColumnName("response_body_json").HasColumnType("jsonb");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.LockedUntil).HasColumnName("locked_until");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

        builder.HasIndex(x => new { x.Scope, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_idempotency_keys_scope_key");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_idempotency_keys_expires_at");
    }
}