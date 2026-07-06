using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class FieldPermissionConfiguration : IEntityTypeConfiguration<FieldPermission>
{
    public void Configure(EntityTypeBuilder<FieldPermission> builder)
    {
        builder.ToTable("field_permissions", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.CanView).HasColumnName("can_view");
        builder.Property(x => x.CanEdit).HasColumnName("can_edit");
        builder.Property(x => x.Effect).HasColumnName("effect").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.CanMask).HasColumnName("can_mask");
        builder.Property(x => x.ConditionJson).HasColumnName("condition_json").HasMaxLength(4096);
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().HasDefaultValue(1L);

        builder.HasIndex(x => new { x.BoardId, x.FieldId }).HasDatabaseName("idx_field_permissions_board_field");
        builder.HasIndex(x => new { x.SubjectType, x.SubjectId }).HasDatabaseName("idx_field_permissions_subject");
    }
}
