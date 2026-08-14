namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
