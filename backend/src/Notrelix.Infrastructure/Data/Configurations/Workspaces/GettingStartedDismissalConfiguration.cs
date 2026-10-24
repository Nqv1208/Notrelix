using Notrelix.Domain.Workspaces.Onboarding;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class GettingStartedDismissalConfiguration : IEntityTypeConfiguration<GettingStartedDismissal>
{
    public void Configure(EntityTypeBuilder<GettingStartedDismissal> builder)
    {
        builder.ToTable("getting_started_dismissals", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.DismissedAt).HasColumnName("dismissed_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique().HasDatabaseName("ux_getting_started_dismissals_member");
    }
}
