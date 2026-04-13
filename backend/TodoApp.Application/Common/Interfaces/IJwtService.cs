using TodoApp.Domain.Entities;

namespace TodoApp.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Guid? ValidateAccessToken(string token);
    }
}
