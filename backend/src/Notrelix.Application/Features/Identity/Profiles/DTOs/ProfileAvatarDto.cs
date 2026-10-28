namespace Notrelix.Application.Features.Identity.Profiles.DTOs;

/// <summary>The caller's profile photo after a change: the address it is served from, or null when removed.</summary>
public sealed record ProfileAvatarDto(string? AvatarUrl);
