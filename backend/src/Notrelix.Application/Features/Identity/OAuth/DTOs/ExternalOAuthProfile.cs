namespace Notrelix.Application.Features.Identity.OAuth.DTOs;

public sealed record ExternalOAuthProfile(
    OAuthProvider Provider,
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? AvatarUrl,
    JsonValue RawProfile);
