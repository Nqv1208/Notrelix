namespace Notrelix.API.Contracts.Integrations.Calendar.Requests;

public record ConnectCalendarRequest(string Provider, string AccessToken, Guid? ProviderAccountId, string SyncDirection);
