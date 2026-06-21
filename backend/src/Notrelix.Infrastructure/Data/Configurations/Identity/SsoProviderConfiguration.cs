using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class SsoProviderConfiguration : IEntityTypeConfiguration<SsoProvider>
{
    public void Configure(EntityTypeBuilder<SsoProvider> builder)
    {
        builder.ToTable("sso_providers", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ProviderType).HasColumnName("provider_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Configuration, config =>
        {
            config.Property(c => c.EntityId).HasColumnName("entity_id").HasMaxLength(512);
            config.Property(c => c.SsoUrl).HasColumnName("sso_url").HasMaxLength(2048);
            config.Property(c => c.CertificateRef).HasColumnName("certificate_ref").HasMaxLength(512);
            config.Property(c => c.Domain).HasColumnName("domain").HasMaxLength(256);
            config.Property(c => c.RedirectUri).HasColumnName("redirect_uri").HasMaxLength(2048);
        });

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_sso_providers_workspace_id");
    }
}
