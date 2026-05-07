using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Interfaces;

namespace Notrelix.Infrastructure.Jwt
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtSettings _jwtSettings;

        public CookieService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> jwtSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _jwtSettings = jwtSettings.Value;
        }

        public void SetTokenCookie(string accesToken, string refreshToken)
        {
            var response = (_httpContextAccessor.HttpContext?.Response) ?? throw new InvalidOperationException("No active HTTP context found.");
            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Uncomment if using HTTPS
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                SameSite = SameSiteMode.Lax,
            };
            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Uncomment if using HTTPS
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
                SameSite = SameSiteMode.Lax,
            };
            response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);
            response.Cookies.Append("accessToken", accesToken, accessTokenOptions);
        }

        public void DeleteTokenCookie()
        {
            var response = (_httpContextAccessor.HttpContext?.Response) ?? throw new InvalidOperationException("No active HTTP context found.");
            response.Cookies.Delete("refreshToken");
            response.Cookies.Delete("accessToken");
        }
    }
}