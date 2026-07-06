namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class AccountConfiguration : IEntityTypeConfiguration<Domain.Accounts.Accounts.Account>
{
    public void Configure(EntityTypeBuilder<Domain.Accounts.Accounts.Account> builder)
    {
        builder.ToTable("accounts", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(160);
        builder.Property(x => x.Slug).HasColumnName("slug").IsRequired().HasMaxLength(128);
        builder.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(240);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(32);
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(32);
        builder.Property(x => x.DefaultRegionCode).HasColumnName("default_region_code").HasMaxLength(32);
        builder.Property(x => x.PlanCode).HasColumnName("plan_code").HasMaxLength(80);

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

        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("idx_account_slug");
    }
}
