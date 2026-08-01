using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Templates;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class ItemTemplateConfiguration : IEntityTypeConfiguration<ItemTemplate>
{
    public void Configure(EntityTypeBuilder<ItemTemplate> builder)
    {
        builder.ToTable("item_templates", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Values).HasColumnName("values").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.Name }).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_item_templates_board_name");
    }
}
