using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public class JobLockConfiguration : IEntityTypeConfiguration<JobLockRecord>
{
    public void Configure(EntityTypeBuilder<JobLockRecord> builder)
    {
        builder.ToTable("job_locks", DbSchemas.Ops);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LockKey).HasColumnName("lock_key").IsRequired().HasMaxLength(240);
        builder.Property(x => x.LockedBy).HasColumnName("locked_by").IsRequired().HasMaxLength(120);
        builder.Property(x => x.FencingToken).HasColumnName("fencing_token").IsRequired().HasDefaultValue(1L);
        builder.Property(x => x.LockedUntil).HasColumnName("locked_until").IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.AcquiredAt).HasColumnName("acquired_at").IsRequired();
        builder.Property(x => x.RenewedAt).HasColumnName("renewed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.LockKey).IsUnique().HasDatabaseName("ix_ops_job_locks_lock_key");
        builder.HasIndex(x => x.LockedUntil).HasDatabaseName("ix_ops_job_locks_locked_until");
        builder.HasIndex(x => new { x.LockedBy, x.LockedUntil }).HasDatabaseName("ix_ops_job_locks_owner");
    }
}
