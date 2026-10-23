using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Domain.Identity.Security;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Security;

/// <summary>
/// EF implementation of the security activity ports. Recording only stages the entry on the request
/// data session's context, so it commits (or rolls back) with the state change it describes.
/// Reading is always filtered by the owning user id.
/// </summary>
internal sealed class SecurityActivityStore : ISecurityActivityRecorder, ISecurityActivityReader
{
    private readonly ApplicationDbContext _context;
    private readonly IClientMetadata _client;

    public SecurityActivityStore(ApplicationDbContext context, IClientMetadata client)
    {
        _context = context;
        _client = client;
    }

    public void Record(Guid userId, SecurityActivityType type, DateTimeOffset occurredAt, Guid? sessionId = null)
    {
        _context.SecurityActivity.Add(SecurityActivityEntry.Record(
            userId, type, occurredAt, _client.IpAddress, _client.UserAgent, sessionId));
    }

    public async Task<IReadOnlyList<SecurityActivityDto>> ListAsync(
        Guid userId,
        SecurityActivityPosition? after,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _context.SecurityActivity.AsNoTracking().Where(e => e.UserId == userId);

        if (after is { } position)
        {
            var at = position.OccurredAt;
            var id = position.Id;
            query = query.Where(e => e.OccurredAt < at || (e.OccurredAt == at && e.Id.CompareTo(id) < 0));
        }

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Take(take)
            .Select(e => new SecurityActivityDto(e.Id, e.Type, e.OccurredAt, e.IpAddress, e.UserAgent, e.SessionId))
            .ToListAsync(cancellationToken);
    }
}
