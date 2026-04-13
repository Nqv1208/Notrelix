using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ListId).HasColumnName("list_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(500);
        builder.Property(x => x.DescriptionMd).HasColumnName("description_md");
        builder.Property(x => x.Position).HasColumnName("position");
        builder.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("open");
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.Cover).HasColumnName("cover").HasColumnType("jsonb");
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.List).WithMany().HasForeignKey(x => x.ListId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ListId, x.Position })
            .HasDatabaseName("idx_cards_list_position")
            .HasFilter("is_deleted = false");

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DomainEvents);
    }
}
