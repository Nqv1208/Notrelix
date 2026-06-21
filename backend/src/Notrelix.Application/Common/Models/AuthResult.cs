namespace Notrelix.Application.Common.Models;

// Response trả về sau khi login/register
public record AuthResult
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required UserDto User { get; init; }
}

// User DTO
public record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public string? AvatarUrl { get; init; }
}
