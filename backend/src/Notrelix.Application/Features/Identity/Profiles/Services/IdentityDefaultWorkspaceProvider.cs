using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Domain.Identity.Profiles;

namespace Notrelix.Application.Features.Identity.Profiles.Services;

/// <summary>
/// Producer-owned implementation of <see cref="IIdentityDefaultWorkspace"/> over the person's
/// profile. It runs inside the caller's request transaction (the pipeline owns the commit).
/// </summary>
public sealed class IdentityDefaultWorkspaceProvider : IIdentityDefaultWorkspace
{
    private readonly IIdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdentityDefaultWorkspaceProvider(IIdentityDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<Guid?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.DefaultWorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SetAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (profile is null)
        {
            profile = UserProfile.Create(userId, now);
            _context.UserProfiles.Add(profile);
        }

        profile.ChangeDefaultWorkspace(workspaceId, now);
    }

    public async Task ClearIfAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is { DefaultWorkspaceId: { } current } && current == workspaceId)
        {
            profile.ChangeDefaultWorkspace(null, _dateTimeProvider.UtcNow);
        }
    }
}
