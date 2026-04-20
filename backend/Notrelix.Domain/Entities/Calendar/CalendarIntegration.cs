using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Calendar;

/// <summary>
/// OAuth integration với external calendar providers (Google Calendar, Outlook...)
/// </summary>
public class CalendarIntegration : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public CalendarProvider Provider { get; private set; }
    public string? ProviderAccountEmail { get; private set; }
    public string AccessToken { get; private set; } = null!;
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }
    public string? CalendarId { get; private set; }
    public SyncDirection SyncDirection { get; private set; } = SyncDirection.Both;
    public DateTime? LastSyncedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private CalendarIntegration() : base() { }

    public static CalendarIntegration Create(
        Guid userId,
        CalendarProvider provider,
        string accessToken,
        string? refreshToken = null,
        Guid? workspaceId = null,
        SyncDirection syncDirection = SyncDirection.Both)
    {
        return new CalendarIntegration
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Provider = provider,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            SyncDirection = syncDirection,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTokens(string accessToken, string? refreshToken, DateTime? expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = expiresAt;
    }

    public void RecordSync() => LastSyncedAt = DateTime.UtcNow;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
