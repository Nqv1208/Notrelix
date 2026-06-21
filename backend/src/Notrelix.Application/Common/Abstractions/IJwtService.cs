using Notrelix.Domain.Identity;

namespace Notrelix.Application.Common.Abstractions
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Guid? ValidateAccessToken(string token);
    }
}
