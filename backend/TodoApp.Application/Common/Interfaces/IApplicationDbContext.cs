using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Common.Interfaces;

// Interface cho DbContext - Application layer tương tác với DB thông qua interface này
public interface IApplicationDbContext
{
    // Identity
    DbSet<User> Users { get; }
    DbSet<Session> Sessions { get; }

    // Workspace
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }

    // Content
    DbSet<ContentItem> ContentItems { get; }
    DbSet<Block> Blocks { get; }
    DbSet<Todo> Todos { get; }

    // Collaboration
    DbSet<Permission> Permissions { get; }
    DbSet<Comment> Comments { get; }

    // Activity
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
