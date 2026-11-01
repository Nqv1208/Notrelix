using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Domain.Identity.Profiles;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.UpdateAccountPreferences;

/// <summary>
/// Partial update of the caller's own account preferences. Omitted members keep their value.
/// <see cref="ExpectedVersion"/> is the <c>version</c> from the last read (0 when none was saved).
/// The request is user-scoped (global), which the pipeline's expected-version constraint does not
/// cover (it requires a Resource/Workspace scope), so the handler compares the version itself
/// and the aggregate's version concurrency token closes the race at the database.
/// </summary>
public sealed record UpdateAccountPreferencesCommand
    : ICommand<Result<AccountPreferencesDto>>, IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    public required long ExpectedVersion { get; init; }
    public string? Locale { get; init; }
    public string? Timezone { get; init; }
    public string? Theme { get; init; }

    /// <summary>E.164-ish phone (see <c>UserPhone</c>); a blank value clears it; omitted keeps it.</summary>
    public string? Phone { get; init; }

    public bool? PhoneVisibleToWorkspaceMembers { get; init; }
}

public sealed class UpdateAccountPreferencesCommandHandler
    : IRequestHandler<UpdateAccountPreferencesCommand, Result<AccountPreferencesDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateAccountPreferencesCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AccountPreferencesDto>> Handle(
        UpdateAccountPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _dateTimeProvider.UtcNow;

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            if (request.ExpectedVersion != 0)
            {
                throw StaleVersion(request.ExpectedVersion, 0);
            }

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return Result<AccountPreferencesDto>.Failure(SessionErrors.SessionUserMissing);
            }

            profile = UserProfile.Create(userId, now);
            _context.UserProfiles.Add(profile);
        }
        else if (profile.Version != request.ExpectedVersion)
        {
            throw StaleVersion(request.ExpectedVersion, profile.Version);
        }

        profile.ApplyAccountPreferences(
            request.Locale, request.Timezone, request.Theme, now, request.Phone, request.PhoneVisibleToWorkspaceMembers);

        return Result<AccountPreferencesDto>.Success(
            new AccountPreferencesDto(profile.Locale, profile.Timezone, profile.Theme, profile.Version, profile.Phone, profile.PhoneVisibleToWorkspaceMembers));
    }

    private static PreconditionFailedException StaleVersion(long expected, long current) => new(
        "Account preferences were changed elsewhere. Reload and retry.",
        "identity.preferences.version-conflict",
        expected,
        current);
}
