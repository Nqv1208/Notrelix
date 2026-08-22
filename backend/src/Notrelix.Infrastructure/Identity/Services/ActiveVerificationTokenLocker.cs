using Notrelix.Application.Features.Identity.Verification.Abstractions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Services;

/// <summary>
/// PostgreSQL implementation using FOR UPDATE to lock active verification tokens.
/// Prevents concurrent token issuance races.
/// </summary>
public sealed class ActiveVerificationTokenLocker : IActiveVerificationTokenLocker
{
    private readonly ApplicationDbContext _context;

    public ActiveVerificationTokenLocker(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmailVerificationToken>> LockActiveTokensAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<EmailVerificationToken>()
            .FromSqlInterpolated($"""
                SELECT *
                FROM identity.email_verification_tokens
                WHERE user_id = {userId}
                  AND status = 'Active'
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
    }
}
