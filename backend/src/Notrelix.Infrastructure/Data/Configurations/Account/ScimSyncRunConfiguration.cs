using Notrelix.Domain.Accounts.Scim;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class ScimSyncRunConfiguration : IEntityTypeConfiguration<ScimSyncRun>
{
    public void Configure(EntityTypeBuilder<ScimSyncRun> builder)
    {
        builder.ToTable("scim_sync_runs", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.DirectoryId).HasColumnName("directory_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(32);
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.UsersCreated).HasColumnName("users_created");
        builder.Property(x => x.UsersUpdated).HasColumnName("users_updated");
        builder.Property(x => x.UsersDisabled).HasColumnName("users_disabled");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
