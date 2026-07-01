using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.ToTable("form_submissions", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.FormId).HasColumnName("form_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.CreatedItemId).HasColumnName("created_item_id");
        builder.Property(x => x.SubmitterUserId).HasColumnName("submitter_user_id");
        builder.Property(x => x.SubmitterEmail).HasColumnName("submitter_email").HasMaxLength(320);
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").IsRequired();
        builder.Property(x => x.SourceIp).HasColumnName("source_ip").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at").IsRequired();
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");

        builder.HasOne<Form>()
            .WithMany()
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FormId).HasDatabaseName("idx_form_submissions_form_id");
        builder.HasIndex(x => x.SubmitterEmail).HasDatabaseName("idx_form_submissions_submitter_email");
    }
}
