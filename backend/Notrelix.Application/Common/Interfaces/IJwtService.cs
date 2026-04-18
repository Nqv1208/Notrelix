using Notrelix.Domain.Entities;

namespace Notrelix.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Guid? ValidateAccessToken(string token);
    }
}
