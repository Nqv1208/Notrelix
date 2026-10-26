using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Profiles.DTOs;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.RemoveAvatar;

/// <summary>
/// Removes the caller's own profile photo (uploaded or external). Removing an absent photo succeeds
/// and changes nothing. The stored object is removed by the avatar object sweep once unreferenced.
/// </summary>
public sealed record RemoveAvatarCommand
    : ICommand<Result<ProfileAvatarDto>>, IWriteRequest, IGlobalRequest, IAuthenticatedRequest;

public sealed class RemoveAvatarCommandHandler : IRequestHandler<RemoveAvatarCommand, Result<ProfileAvatarDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveAvatarCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ProfileAvatarDto>> Handle(RemoveAvatarCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<ProfileAvatarDto>.Failure(new ApplicationError(
                "identity.auth.user-not-found", "User not found.", ApplicationErrorType.NotFound));
        }

        user.RemoveAvatar(userId, _dateTimeProvider.UtcNow);

        return Result<ProfileAvatarDto>.Success(new ProfileAvatarDto(null));
    }
}
