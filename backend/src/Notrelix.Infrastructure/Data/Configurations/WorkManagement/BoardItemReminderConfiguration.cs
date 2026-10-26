using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemReminderConfiguration : IEntityTypeConfiguration<BoardItemReminder>
{
    public void Configure(EntityTypeBuilder<BoardItemReminder> builder)
    {
        builder.ToTable("board_item_reminders", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.SenderUserId).HasColumnName("sender_user_id").IsRequired();
        builder.Property(x => x.RemindedAt).HasColumnName("reminded_at").IsRequired();

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per Item and sender: the row is the 24 hour limit.
        builder.HasIndex(x => new { x.ItemId, x.SenderUserId }).IsUnique().HasDatabaseName("idx_board_item_reminders_item_sender");
        builder.HasIndex(x => x.SenderUserId).HasDatabaseName("idx_board_item_reminders_sender");
    }
}
