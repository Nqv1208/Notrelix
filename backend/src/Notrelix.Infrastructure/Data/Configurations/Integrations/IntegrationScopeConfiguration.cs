using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class IntegrationScopeConfiguration : IEntityTypeConfiguration<IntegrationScope>
{
    public void Configure(EntityTypeBuilder<IntegrationScope> builder)
    {
        builder.ToTable("integration_scopes", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ConnectionId).HasColumnName("connection_id").IsRequired();
        builder.Property(x => x.Scope).HasColumnName("scope").IsRequired().HasMaxLength(256);

        builder.HasIndex(x => x.ConnectionId).HasDatabaseName("idx_integration_scopes_connection_id");
    }
}
