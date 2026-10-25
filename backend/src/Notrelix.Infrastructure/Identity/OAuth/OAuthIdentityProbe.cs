using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.OAuth;

/// <summary>Answers through the owner-defined <c>ops.oauth_identity_linked</c> function: a boolean, never a row.</summary>
public sealed class OAuthIdentityProbe : IOAuthIdentityProbe
{
    private readonly ApplicationDbContext _context;

    public OAuthIdentityProbe(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsLinkedAsync(string provider, string subject, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<bool>($"""SELECT ops.oauth_identity_linked({provider}, {subject}) AS "Value" """)
            .ToListAsync(cancellationToken);
        return rows.Count == 1 && rows[0];
    }
}
