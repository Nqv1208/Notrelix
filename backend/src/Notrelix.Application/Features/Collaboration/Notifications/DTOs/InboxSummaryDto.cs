namespace Notrelix.Application.Features.Collaboration.Notifications.DTOs;

/// <summary>Unread notifications the caller holds in one Workspace.</summary>
public sealed record InboxWorkspaceUnreadDto(Guid WorkspaceId, int Unread);

/// <summary>The caller's unread notification counts per Workspace and in total.</summary>
public sealed record InboxSummaryDto(IReadOnlyList<InboxWorkspaceUnreadDto> PerWorkspace, int Total);
