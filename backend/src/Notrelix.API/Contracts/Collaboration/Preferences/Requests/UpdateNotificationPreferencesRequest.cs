namespace Notrelix.API.Contracts.Collaboration.Preferences.Requests;

/// <param name="Type">A notification type code: assigned, mention, approval, overdue, watching, invitation or digest.</param>
/// <param name="InApp">The in-app channel for this type; omitted keeps its value.</param>
/// <param name="Email">The email channel for this type; omitted keeps its value.</param>
public record NotificationPreferenceRowRequest(string Type, bool? InApp, bool? Email);

/// <param name="ExpectedVersion">The <c>version</c> of the last read (0 when nothing was saved); required.</param>
/// <param name="Rows">The types to change, each with the channels to set; required, at least one channel overall.</param>
public record UpdateNotificationPreferencesRequest(long? ExpectedVersion, IReadOnlyList<NotificationPreferenceRowRequest>? Rows);
