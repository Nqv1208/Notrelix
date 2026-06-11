using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Checklists;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("checklist_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ChecklistId).HasColumnName("checklist_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(512);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.AssigneeUserId).HasColumnName("assignee_user_id");
        builder.Property(x => x.DueAt).HasColumnName("due_at");
        builder.Property(x => x.Position).HasColumnName("position").HasColumnType("float8").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

        builder.HasOne<Checklist>()
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ChecklistId, x.Position }).HasDatabaseName("idx_checklist_items_checklist_position");
    }
}
