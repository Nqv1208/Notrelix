using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Auth.Jwt;

namespace Notrelix.Infrastructure.Auth.Cookies
{
    public class CookieService : ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _webHostEnvironment;
        private readonly JwtSettings _jwtSettings;

        public CookieService(IHttpContextAccessor httpContextAccessor, IHostEnvironment webHostEnvironment, IOptions<JwtSettings> jwtSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _webHostEnvironment = webHostEnvironment;
            _jwtSettings = jwtSettings.Value;
        }

        public void SetTokenCookie(string accesToken, string refreshToken)
        {
            var response = (_httpContextAccessor.HttpContext?.Response) ?? throw new InvalidOperationException("No active HTTP context found.");
            var isProduction = _webHostEnvironment.IsProduction();

            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction, // Uncomment if using HTTPS
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
            };
            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction, // Uncomment if using HTTPS
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDays),
                SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
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