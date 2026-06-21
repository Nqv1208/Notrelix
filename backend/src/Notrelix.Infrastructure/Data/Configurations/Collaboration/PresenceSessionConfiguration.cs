using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Presence;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class PresenceSessionConfiguration : IEntityTypeConfiguration<PresenceSession>
{
    public void Configure(EntityTypeBuilder<PresenceSession> builder)
    {
        builder.ToTable("presence_sessions", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ConnectionId).HasColumnName("connection_id").HasMaxLength(256);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId }).HasDatabaseName("idx_presence_sessions_workspace_user");
        builder.HasIndex(x => x.LastSeenAt).HasDatabaseName("idx_presence_sessions_last_seen_at");
    }
}
