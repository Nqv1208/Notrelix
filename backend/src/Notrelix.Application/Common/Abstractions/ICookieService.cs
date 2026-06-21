
namespace Notrelix.Application.Common.Abstractions
{
    public interface ICookieService
    {
        void SetTokenCookie(string accesToken, string refreshToken);
        void DeleteTokenCookie();
    }
}