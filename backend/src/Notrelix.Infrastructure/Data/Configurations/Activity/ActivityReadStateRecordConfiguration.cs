using Notrelix.Infrastructure.Data.Projections.Activity;

namespace Notrelix.Infrastructure.Data.Configurations.Activity;

public sealed class ActivityReadStateRecordConfiguration : IEntityTypeConfiguration<ActivityReadStateRecord>
{
    public void Configure(EntityTypeBuilder<ActivityReadStateRecord> builder)
    {
        builder.ToTable("activity_read_states", DbSchemas.Activity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.LastReadAt).IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique();
    }
}
