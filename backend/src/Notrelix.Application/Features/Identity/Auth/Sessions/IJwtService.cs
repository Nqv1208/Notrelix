namespace Notrelix.Application.Features.Identity.Auth.Sessions;

public interface IJwtService
{
    string GenerateAccessToken(User user, Guid? sessionId = null);
    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}
