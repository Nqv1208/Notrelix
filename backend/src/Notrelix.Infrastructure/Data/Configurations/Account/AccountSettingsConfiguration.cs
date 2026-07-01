using Notrelix.Domain.Accounts.Settings;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class AccountSettingsConfiguration : IEntityTypeConfiguration<AccountSettings>
{
    public void Configure(EntityTypeBuilder<AccountSettings> builder)
    {
        builder.ToTable("account_settings", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.SettingKey).HasColumnName("setting_key").IsRequired().HasMaxLength(120);
        builder.Property(x => x.SettingValue).HasColumnName("setting_value").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.AccountId, x.SettingKey }).IsUnique().HasDatabaseName("idx_account_settings_key");
    }
}
