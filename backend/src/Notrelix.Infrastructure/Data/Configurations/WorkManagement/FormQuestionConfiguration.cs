using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Forms;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class FormQuestionConfiguration : IEntityTypeConfiguration<FormQuestion>
{
    public void Configure(EntityTypeBuilder<FormQuestion> builder)
    {
        builder.ToTable("form_questions", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.FormId).HasColumnName("form_id").IsRequired();
        builder.Property(x => x.BoardFieldId).HasColumnName("board_field_id");
        builder.Property(x => x.QuestionKey).HasColumnName("question_key").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Label).HasColumnName("label").IsRequired().HasMaxLength(512);
        builder.Property(x => x.QuestionType).HasColumnName("question_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsRequired).HasColumnName("is_required");

        builder.Property(x => x.Position).HasColumnName("position").HasMaxLength(50).IsRequired();

        builder.OwnsOne(x => x.Config, config =>
        {
            config.ToJson();
        });
        builder.Property(x => x.Version).HasColumnName("version");

        builder.HasOne<Form>()
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FormId, x.QuestionKey }).IsUnique().HasDatabaseName("idx_form_questions_form_key");
        builder.HasIndex(x => new { x.FormId, x.Position }).HasDatabaseName("idx_form_questions_form_position");
    }
}
