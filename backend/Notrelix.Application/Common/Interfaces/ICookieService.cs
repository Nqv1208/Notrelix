
namespace Notrelix.Application.Common.Interfaces
{
    public interface ICookieService
    {
        void SetTokenCookie(string accesToken, string refreshToken);
        void DeleteTokenCookie();
    }
}