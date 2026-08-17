namespace Notrelix.Application.Common.Security.Auth;

public interface IJwtBlacklistService
{
    Task BlacklistAsync(string jti, TimeSpan expiration);
    Task<bool> IsBlacklistedAsync(string jti);
    Task<DateTimeOffset?> GetUserRevokedBeforeAsync(Guid userId);
    Task RevokeUserBeforeAsync(Guid userId, DateTimeOffset revokedBefore, TimeSpan expiration);

    Task<DateTimeOffset?> GetSessionRevokedBeforeAsync(Guid sessionId);
    Task RevokeSessionBeforeAsync(Guid sessionId, DateTimeOffset revokedBefore, TimeSpan expiration);
}
