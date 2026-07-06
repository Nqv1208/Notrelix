namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string Name);
