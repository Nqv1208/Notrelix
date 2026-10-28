using System.Text.Json.Serialization;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Application.Features.Identity.Profiles.Services;

namespace Notrelix.Application.Features.Identity.Profiles.Commands.UploadAvatar;

/// <summary>
/// Replaces the caller's own profile photo with an uploaded image. The subject is always the
/// principal. The body is a forward-only stream so Application stays free of transport types; the
/// accepted format is derived from the bytes (PNG, JPEG or WebP), never from a client-declared type.
/// The replaced object is left unreferenced and removed by the avatar object sweep: deleting it here
/// could lose the old photo if the request later rolls back.
/// </summary>
public sealed record UploadAvatarCommand(
    [property: JsonIgnore] Stream Content)
    : ICommand<Result<ProfileAvatarDto>>, IWriteRequest, IGlobalRequest, IAuthenticatedRequest;

public sealed class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<ProfileAvatarDto>>
{
    public const string UnsupportedErrorCode = "identity.avatar.unsupported-format";
    public const string TooLargeErrorCode = "identity.avatar.too-large";

    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IStorageService _storage;
    private readonly IRequestRollbackCompensations _compensations;

    public UploadAvatarCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider,
        IStorageService storage,
        IRequestRollbackCompensations compensations)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _storage = storage;
        _compensations = compensations;
    }

    public async Task<Result<ProfileAvatarDto>> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<ProfileAvatarDto>.Failure(new ApplicationError(
                "identity.auth.user-not-found", "User not found.", ApplicationErrorType.NotFound));
        }

        var head = new byte[AvatarImage.SniffLength];
        var headLength = await request.Content.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
        var format = AvatarImage.Classify(head.AsSpan(0, headLength))
            ?? throw new UnsupportedMediaTypeException(
                "A profile photo must be a PNG, JPEG or WebP image.", UnsupportedErrorCode);

        var storageKey = AvatarStorageKey.Create(userId, format);

        // Registered before the write so a partial or later-rolled-back object is removed when the
        // request fails before commit.
        _compensations.Register(
            $"delete avatar object {storageKey}",
            token => _storage.DeleteAsync(storageKey, token));

        using var body = new SizeLimitedReadStream(
            new PrefixedReadStream(head.AsMemory(0, headLength), request.Content),
            AvatarImage.MaxBytes);
        try
        {
            await _storage.WriteAsync(storageKey, body, format.ContentType, ct);
        }
        catch (PayloadTooLargeException)
        {
            throw new PayloadTooLargeException(AvatarImage.MaxBytes, TooLargeErrorCode);
        }

        var avatarUrl = AvatarStorageKey.UrlFor(userId, storageKey);
        user.ChangeUploadedAvatar(storageKey, avatarUrl, userId, _dateTimeProvider.UtcNow);

        return Result<ProfileAvatarDto>.Success(new ProfileAvatarDto(avatarUrl));
    }
}
