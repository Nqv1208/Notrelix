
namespace Notrelix.Application.Common.Security.Auth
{
    public interface ICookieService
    {
        void SetTokenCookie(string accesToken, string refreshToken);
        void DeleteTokenCookie();
    }
}