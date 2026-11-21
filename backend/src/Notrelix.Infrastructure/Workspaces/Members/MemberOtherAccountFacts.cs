using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Workspaces.Members;

/// <summary>Asks the bounded database function; the application role cannot read another Account's membership rows.</summary>
public sealed class MemberOtherAccountFacts : IMemberOtherAccountFacts
{
    private readonly ApplicationDbContext _context;

    public MemberOtherAccountFacts(ApplicationDbContext context) => _context = context;

    public Task<bool> IsActiveInAnotherAccountAsync(
        Guid accountId, Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        _context.Database
            .SqlQuery<bool>($"SELECT ops.member_active_elsewhere({accountId}, {workspaceId}, {userId}) AS \"Value\"")
            .SingleAsync(cancellationToken);
}
