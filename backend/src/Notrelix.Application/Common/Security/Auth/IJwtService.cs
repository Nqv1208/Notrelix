namespace Notrelix.Application.Common.Security.Auth;

public interface IJwtService
{
    string GenerateAccessToken(User user, Guid? sessionId = null);
    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}
