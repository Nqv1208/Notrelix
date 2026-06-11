using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Workspaces.Teams;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.TeamId).HasColumnName("team_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.HasOne<Team>()
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.TeamId);

        builder.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique().HasDatabaseName("idx_team_members_team_user");
    }
}
