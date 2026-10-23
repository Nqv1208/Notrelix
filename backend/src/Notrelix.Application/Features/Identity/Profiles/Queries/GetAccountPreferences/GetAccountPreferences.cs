using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Profiles.DTOs;

namespace Notrelix.Application.Features.Identity.Profiles.Queries.GetAccountPreferences;

/// <summary>Reads the caller's own account preferences. The subject is always the principal.</summary>
public sealed record GetAccountPreferencesQuery
    : IQuery<Result<AccountPreferencesDto>>, IReadRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class GetAccountPreferencesQueryHandler
    : IRequestHandler<GetAccountPreferencesQuery, Result<AccountPreferencesDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;

    public GetAccountPreferencesQueryHandler(IIdentityDbContext context, ICurrentRequestContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AccountPreferencesDto>> Handle(
        GetAccountPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var profile = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        // A profile row is created by the first save; until then the defaults apply.
        return Result<AccountPreferencesDto>.Success(profile is null
            ? AccountPreferencesDto.Defaults
            : new AccountPreferencesDto(profile.Locale, profile.Timezone, profile.Theme, profile.Version, profile.Phone, profile.PhoneVisibleToWorkspaceMembers));
    }
}
