using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        builder.Property(s => s.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(s => s.DeviceInfo)
            .HasColumnName("device_info")
            .HasMaxLength(500);

        builder.Property(s => s.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(s => s.RefreshToken)
            .IsUnique();

        builder.HasIndex(s => s.UserId);

        // Ignore domain events
        builder.Ignore(s => s.DomainEvents);
    }
}
