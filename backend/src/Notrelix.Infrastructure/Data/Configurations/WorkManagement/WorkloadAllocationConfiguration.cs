using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Workload;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class WorkloadAllocationConfiguration : IEntityTypeConfiguration<WorkloadAllocation>
{
    public void Configure(EntityTypeBuilder<WorkloadAllocation> builder)
    {
        builder.ToTable("workload_allocations", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id");
        builder.Property(x => x.ItemId).HasColumnName("item_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AllocationDate).HasColumnName("allocation_date").HasColumnType("date").IsRequired();
        builder.Property(x => x.AllocatedMinutes).HasColumnName("allocated_minutes");
        builder.Property(x => x.Version).HasColumnName("version");

        builder.HasIndex(x => new { x.UserId, x.AllocationDate }).HasDatabaseName("idx_workload_allocations_user_date");
        builder.HasIndex(x => x.ItemId).HasDatabaseName("idx_workload_allocations_item_id");
    }
}
