using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Accounts.Scim;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class ScimDirectoryConfiguration : IEntityTypeConfiguration<ScimDirectory>
{
    public void Configure(EntityTypeBuilder<ScimDirectory> builder)
    {
        builder.ToTable("scim_directories", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.IdentityProviderId).HasColumnName("identity_provider_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
        builder.Property(x => x.BaseUrl).HasColumnName("base_url");
        builder.Property(x => x.BearerTokenHash).HasColumnName("bearer_token_hash").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(32);
        builder.Property(x => x.LastSyncAt).HasColumnName("last_sync_at");

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

        builder.HasIndex(x => new { x.AccountId, x.Name }).IsUnique().HasDatabaseName("idx_scim_directories_account_name");
    }
}
