using Notrelix.API.Contracts.Identity.Profile;
using Notrelix.API.Extensions;
using Notrelix.API.Uploads;
using Notrelix.Application.Features.Identity.Profiles.Commands.RemoveAvatar;
using Notrelix.Application.Features.Identity.Profiles.Commands.UploadAvatar;
using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Application.Features.Identity.Profiles.Services;

namespace Notrelix.API.Endpoints.Identity.Profile.Commands;

/// <summary>
/// The caller's own profile photo: replace it with an uploaded image, or remove it. The photo itself
/// is read through <c>GET /api/v1/users/{userId}/avatar</c>, which is authorised.
/// </summary>
public static class AvatarEndpoints
{
    public static IEndpointRouteBuilder MapAvatar(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPut("/avatar", UploadAsync)
            .WithName("Identity.Profile.UploadAvatar")
            .WithTags("Identity.Profile")
            .WithSummary("Replace your profile photo (multipart/form-data, streamed)")
            .WithDescription("One image in the part named 'file': PNG, JPEG or WebP, at most 2,000,000 bytes, recognised from its bytes. Returns the address the photo is served from.")
            .Accepts<UploadAvatarForm>("multipart/form-data")
            .Produces<ProfileAvatarDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapAuthenticatedDelete("/avatar", RemoveAsync)
            .WithName("Identity.Profile.RemoveAvatar")
            .WithTags("Identity.Profile")
            .WithSummary("Remove your profile photo")
            .WithDescription("Removing an absent photo succeeds and changes nothing.")
            .Produces<ProfileAvatarDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> UploadAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var part = await MultipartUpload.ReadFilePartAsync(
            httpContext,
            AvatarImage.MaxBytes,
            UploadAvatarCommandHandler.UnsupportedErrorCode,
            UploadAvatarCommandHandler.TooLargeErrorCode,
            cancellationToken);
        if (part.Failure is not null)
        {
            return part.Failure;
        }

        var result = await sender.Send(new UploadAvatarCommand(part.Body!), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> RemoveAsync(ISender sender)
        => (await sender.Send(new RemoveAvatarCommand())).ToApiResult();
}
