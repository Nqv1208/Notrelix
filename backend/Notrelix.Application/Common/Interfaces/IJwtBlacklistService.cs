namespace Notrelix.Application.Common.Interfaces;

public interface IJwtBlacklistService
{
    Task BlacklistAsync(string jti, TimeSpan expiration);
    Task<bool> IsBlacklistedAsync(string jti);
}
