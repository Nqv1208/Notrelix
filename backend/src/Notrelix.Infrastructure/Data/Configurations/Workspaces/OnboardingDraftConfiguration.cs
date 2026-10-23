using Notrelix.Domain.Workspaces.Onboarding;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class OnboardingDraftConfiguration : IEntityTypeConfiguration<OnboardingDraft>
{
    public void Configure(EntityTypeBuilder<OnboardingDraft> builder)
    {
        builder.ToTable("onboarding_drafts", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Step).HasColumnName("step").HasMaxLength(OnboardingDraft.MaxStepLength).IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ux_onboarding_drafts_user");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_onboarding_drafts_expires");
    }
}
