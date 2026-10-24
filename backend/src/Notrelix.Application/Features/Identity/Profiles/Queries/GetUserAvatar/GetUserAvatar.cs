using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Profiles.Ports;
using Notrelix.Application.Features.Identity.Profiles.Services;

namespace Notrelix.Application.Features.Identity.Profiles.Queries.GetUserAvatar;

/// <summary>The stored bytes of a profile photo; the caller disposes <see cref="Content"/>.</summary>
public sealed record UserAvatarContentDto(Stream Content, string ContentType);

/// <summary>
/// Opens the profile photo of a user. Visible to the person and to people who share an active
/// Workspace with them; every other reader, and every user without an uploaded photo, gets the same
/// not-found answer so the endpoint never reveals who has a photo or who exists.
/// </summary>
public sealed record GetUserAvatarQuery(Guid UserId)
    : IQuery<Result<UserAvatarContentDto>>, IReadRequest, IGlobalRequest, IAuthenticatedRequest;

public sealed class GetUserAvatarQueryHandler : IRequestHandler<GetUserAvatarQuery, Result<UserAvatarContentDto>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IAvatarAudienceReadPort _audience;
    private readonly IStorageService _storage;
    private readonly ILogger<GetUserAvatarQueryHandler> _logger;

    public GetUserAvatarQueryHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IAvatarAudienceReadPort audience,
        IStorageService storage,
        ILogger<GetUserAvatarQueryHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _audience = audience;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<UserAvatarContentDto>> Handle(GetUserAvatarQuery request, CancellationToken ct)
    {
        var viewerId = _currentUser.UserId;

        var key = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.AvatarStorageKey)
            .FirstOrDefaultAsync(ct);

        if (key is null
            || (viewerId != request.UserId && !await _audience.ShareActiveWorkspaceAsync(viewerId, request.UserId, ct)))
        {
            throw new NotFoundException("Avatar", request.UserId);
        }

        if (!AvatarStorageKey.BelongsTo(key, request.UserId))
        {
            _logger.LogError("User {UserId} has an avatar key outside its owner prefix; refusing to open it.", request.UserId);
            throw new NotFoundException("Avatar", request.UserId);
        }

        var content = await _storage.OpenReadAsync(key, ct);
        if (content is null)
        {
            _logger.LogError("User {UserId} has an avatar row but its stored object is missing.", request.UserId);
            throw new NotFoundException("Avatar", request.UserId);
        }

        return Result<UserAvatarContentDto>.Success(new UserAvatarContentDto(content, AvatarStorageKey.ContentTypeOf(key)));
    }
}
