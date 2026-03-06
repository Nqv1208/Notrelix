namespace TodoApp.Infrastructure.Jwt;
public class JwtSettings
{
    public string Audience { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string SecretKey { get; init; } = null!;
    public int ExpireMinutes { get; init; }
    public int RefreshTokenExpireDays { get; init; }
}