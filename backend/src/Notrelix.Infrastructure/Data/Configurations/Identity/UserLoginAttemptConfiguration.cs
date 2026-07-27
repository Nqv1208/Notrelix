using Notrelix.Domain.Identity.Security;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class UserLoginAttemptConfiguration : IEntityTypeConfiguration<UserLoginAttempt>
{
    public void Configure(EntityTypeBuilder<UserLoginAttempt> builder)
    {
        builder.ToTable("user_login_attempts", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.AttemptedEmail).HasColumnName("attempted_email").HasMaxLength(256);
        builder.Property(x => x.Succeeded).HasColumnName("succeeded");
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason");
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_user_login_attempts_user_id");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("idx_user_login_attempts_occurred_at");
    }
}
