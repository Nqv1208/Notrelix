namespace Notrelix.Infrastructure.ReadModels.WorkManagement;

/// <summary>
/// Skeleton for the optimized board-schema read model (v4 §3). Real
/// implementation will join board + fields + views with AsNoTracking and
/// return a read DTO (no tracked EF entities, permission-aware). Not yet wired.
/// </summary>
public sealed class BoardSchemaReadService
{
    // TODO(v4 §3): inject IApplicationDbContext, project board schema for a
    // board+user with AsNoTracking, return a read DTO. Add the Application-side
    // interface (IBoardSchemaReadService) when this is implemented.
}
