using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
    {
        builder.ToTable("forms", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Slug).HasColumnName("slug").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.Visibility).HasColumnName("visibility").IsRequired();
        builder.Property(x => x.SettingsJson).HasColumnName("settings_json").IsRequired();
        builder.Property(x => x.SubmitterPolicyJson).HasColumnName("submitter_policy_json").IsRequired();

        builder.HasMany(x => x.Questions)
            .WithOne()
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.Version).HasColumnName("version");

        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("idx_forms_slug");
        builder.HasIndex(x => x.BoardId).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_forms_board_id");
    }
}
