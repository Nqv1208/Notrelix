namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public sealed record LoginRequest(
    string Email,
    string Password);
