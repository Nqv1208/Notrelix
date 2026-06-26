using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardFieldConfiguration : IEntityTypeConfiguration<BoardField>
{
    public void Configure(EntityTypeBuilder<BoardField> builder)
    {
        builder.ToTable("board_fields", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.DefaultValue).HasColumnName("default_value");
        builder.Property(x => x.IsSystem).HasColumnName("is_system");

        builder.OwnsOne(x => x.Settings, settings =>
        {
            settings.Property(s => s.Data).HasColumnName("settings").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        });

        builder.Property(x => x.Position).HasColumnName("position").IsRequired();

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

        builder.HasMany(x => x.Options)
            .WithOne()
            .HasForeignKey(x => x.FieldId);

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.Position }).HasDatabaseName("idx_board_fields_board_position");
    }
}
