using Notrelix.Infrastructure.Operations.Idempotency;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records", DbSchemas.Ops);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Scope).HasColumnName("scope").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Operation).HasColumnName("operation").IsRequired().HasMaxLength(256);
        builder.Property(x => x.KeyHash).HasColumnName("key_hash").IsRequired().HasMaxLength(64);
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").IsRequired().HasMaxLength(64);

        builder.Property(x => x.State).HasColumnName("state").IsRequired().HasMaxLength(20);

        builder.Property(x => x.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
        builder.Property(x => x.ResultContract).HasColumnName("result_contract").HasMaxLength(512);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();

        builder.HasIndex(x => new { x.Scope, x.Operation, x.KeyHash })
            .IsUnique()
            .HasDatabaseName("ix_idempotency_records_scope_op_key");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_idempotency_records_expires_at");
    }
}
