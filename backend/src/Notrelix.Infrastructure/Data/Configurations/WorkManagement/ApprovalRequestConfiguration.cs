using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("approval_requests", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(512);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id").IsRequired();

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.Kind).HasColumnName("target_type").HasConversion(v => v.Value, v => LegacyResourceTypeMappings.ParseResourceKind(v)).IsRequired().HasMaxLength(128);
            target.Property(t => t.ResourceId).HasColumnName("target_id").IsRequired();
        });

        builder.HasMany(x => x.Steps)
            .WithOne()
            .HasForeignKey(x => x.ApprovalRequestId)
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
    }
}
