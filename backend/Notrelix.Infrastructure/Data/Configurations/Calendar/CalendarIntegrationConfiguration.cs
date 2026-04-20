using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Calendar;
using Notrelix.Domain.Enums;

namespace Notrelix.Infrastructure.Data.Configurations.Calendar;

public class CalendarIntegrationConfiguration : IEntityTypeConfiguration<CalendarIntegration>
{
    public void Configure(EntityTypeBuilder<CalendarIntegration> builder)
    {
        builder.ToTable("calendar_integrations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.ProviderAccountEmail)
            .HasMaxLength(255);

        builder.Property(e => e.AccessToken)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.RefreshToken)
            .HasColumnType("text");

        builder.Property(e => e.CalendarId)
            .HasMaxLength(500);

        builder.Property(e => e.SyncDirection)
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(SyncDirection.Both);

        builder.HasIndex(e => new { e.UserId, e.Provider })
            .IsUnique();

        builder.HasIndex(e => e.WorkspaceId);
    }
}
