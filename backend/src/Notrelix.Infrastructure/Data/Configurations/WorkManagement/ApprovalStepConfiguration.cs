using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("approval_steps", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ApprovalRequestId).HasColumnName("approval_request_id").IsRequired();
        builder.Property(x => x.ApproverUserId).HasColumnName("approver_user_id");
        builder.Property(x => x.ApproverTeamId).HasColumnName("approver_team_id");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.Position).HasColumnName("position");
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at");
        builder.Property(x => x.Note).HasColumnName("note");

        builder.HasOne<ApprovalRequest>()
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApprovalRequestId).HasDatabaseName("idx_approval_steps_request_id");
    }
}
