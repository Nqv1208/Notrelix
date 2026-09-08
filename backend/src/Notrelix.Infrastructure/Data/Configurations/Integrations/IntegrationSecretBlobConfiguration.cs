using Notrelix.Infrastructure.Data.Integrations;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

/// <summary>
/// M8 — technical physical-secret blob persistence. Infrastructure state in
/// the integrations schema; deliberately minimal (no tenant columns — access
/// is exclusively through the opaque reference held by
/// <c>IntegrationSecretVersion</c> rows, which are tenant-scoped and
/// RLS-protected).
/// </summary>
public class IntegrationSecretBlobConfiguration : IEntityTypeConfiguration<IntegrationSecretBlob>
{
    public void Configure(EntityTypeBuilder<IntegrationSecretBlob> builder)
    {
        builder.ToTable("integration_secret_blobs", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.EncryptedPayload).HasColumnName("encrypted_payload").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.Revoked).HasColumnName("revoked");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");

        builder.HasIndex(x => x.Revoked).HasDatabaseName("idx_integration_secret_blobs_revoked");
    }
}
