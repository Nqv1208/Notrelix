using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Public.ApiTokens;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.ApiTokens.Services;

/// <summary>
/// Producer-owned implementation of <see cref="IIdentityApiTokens"/>. It runs inside the caller's request
/// transaction (the pipeline owns the commit).
/// </summary>
public sealed class IdentityApiTokensProvider : IIdentityApiTokens
{
    private readonly IIdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdentityApiTokensProvider(IIdentityDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> RevokeForMemberAsync(
        Guid userId,
        Guid workspaceId,
        Guid revokedBy,
        ApiTokenRevokedReason reason,
        CancellationToken cancellationToken)
    {
        var tokens = await _context.ApiTokens
            .Where(t => t.WorkspaceId == workspaceId && t.UserId == userId && t.Status == ApiTokenStatus.Active)
            .ToListAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        foreach (var token in tokens)
        {
            token.Revoke(revokedBy, now, reason);
        }

        return tokens.Count;
    }
}
