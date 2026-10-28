using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Public.Facts;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.Identity.Registration.Ports;
using Notrelix.Application.Features.Identity.Users.Ports;

namespace Notrelix.Application.Features.Identity.Users.Services;

/// <summary>
/// Producer-owned implementation of the Identity public user facts surface.
/// Maps Identity lifecycle state into stable participation semantics inside
/// Application, where that business policy belongs.
/// </summary>
public sealed class IdentityUserFactsProvider : IIdentityUserFacts
{
    private readonly IIdentityDbContext _context;
    private readonly IEmailRegistrationProbe? _registrationProbe;
    private readonly IMemberEmailReadPort? _emailReadPort;

    public IdentityUserFactsProvider(
        IIdentityDbContext context,
        IEmailRegistrationProbe? registrationProbe = null,
        IMemberEmailReadPort? emailReadPort = null)
    {
        _context = context;
        _registrationProbe = registrationProbe;
        _emailReadPort = emailReadPort;
    }

    public async Task<IdentityUserFact?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.NormalizedEmail,
                u.EmailConfirmed,
                u.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
            return null;

        var canParticipate = snapshot.Status is UserStatus.Active or UserStatus.PendingVerification;

        return new IdentityUserFact(
            snapshot.Id,
            snapshot.NormalizedEmail,
            snapshot.EmailConfirmed,
            canParticipate);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> FindEmailsAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string>();

        if (_emailReadPort is not null)
            return await _emailReadPort.FindAsync(workspaceId, userIds, cancellationToken);

        var rows = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.NormalizedEmail })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => r.NormalizedEmail);
    }

    public Task<string?> FindLocaleAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (string?)p.Locale)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (_registrationProbe is not null)
            return _registrationProbe.IsRegisteredAsync(normalized, cancellationToken);

        return _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }
}
