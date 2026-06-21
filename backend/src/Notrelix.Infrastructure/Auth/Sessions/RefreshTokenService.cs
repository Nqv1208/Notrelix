namespace Notrelix.Infrastructure.Auth.Sessions;

/// <summary>
/// Skeleton refresh-token service (v4 §8.2). Real implementation issues/rotates
/// refresh tokens and stores only their HASH (never the raw token), validating
/// against the persisted session. Not yet wired — refresh logic currently lives
/// inline in JwtService.GenerateRefreshToken.
/// </summary>
public sealed class RefreshTokenService
{
    // TODO(v4 §8.2): Issue/Rotate/Validate refresh tokens; persist hashed token
    // on the session; use Security.Hashing.TokenHasher.
}
