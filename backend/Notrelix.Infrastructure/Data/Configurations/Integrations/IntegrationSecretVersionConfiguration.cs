using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class IntegrationSecretVersionConfiguration : IEntityTypeConfiguration<IntegrationSecretVersion>
{
    public void Configure(EntityTypeBuilder<IntegrationSecretVersion> builder)
    {
        builder.ToTable("integration_secret_versions", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ConnectionId).HasColumnName("connection_id").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired().HasMaxLength(100);
        builder.Property(x => x.SecretReference).HasColumnName("secret_reference").HasConversion<SecretRefConverter>().IsRequired().HasMaxLength(1024);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => x.ConnectionId).HasDatabaseName("idx_integration_secret_versions_connection_id");
    }
}
