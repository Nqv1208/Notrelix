using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities;

namespace Notrelix.Infrastructure.Data.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(x => x.FileName).HasColumnName("filename").HasMaxLength(500);
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(1000);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => new { x.ResourceType, x.ResourceId });
        builder.Ignore(x => x.DomainEvents);
    }
}
