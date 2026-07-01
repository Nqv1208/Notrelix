using Notrelix.Domain.Accounts.Regions;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class AccountRegionConfiguration : IEntityTypeConfiguration<AccountRegion>
{
    public void Configure(EntityTypeBuilder<AccountRegion> builder)
    {
        builder.ToTable("account_regions", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.RegionCode).HasColumnName("region_code").IsRequired().HasMaxLength(32);
        builder.Property(x => x.DataResidencyMode).HasColumnName("data_residency_mode").IsRequired().HasMaxLength(32);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary");
        builder.Property(x => x.MigrationStatus).HasColumnName("migration_status").HasMaxLength(32);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.AccountId, x.RegionCode }).IsUnique().HasDatabaseName("idx_account_regions_code");
    }
}
