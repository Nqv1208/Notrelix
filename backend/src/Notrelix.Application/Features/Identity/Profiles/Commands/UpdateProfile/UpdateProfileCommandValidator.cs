namespace Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(User.MaxNameLength);

        // An external photo is an absolute https address with no credentials in it. The photo the
        // person uploads is set by the upload endpoint and served from Identity's own address.
        RuleFor(x => x.Avatar)
            .MaximumLength(500)
            .Must(IsExternalPhotoAddress)
            .When(x => x.Avatar is not null)
            .WithMessage("The photo address must be an absolute https address.");
    }

    private static bool IsExternalPhotoAddress(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo);
}
