namespace Notrelix.Application.Common.Security.Auth;

public interface IJwtBlacklistService
{
    Task BlacklistAsync(string jti, TimeSpan expiration);
    Task<bool> IsBlacklistedAsync(string jti);
}
