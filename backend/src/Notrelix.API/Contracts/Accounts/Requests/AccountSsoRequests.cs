namespace Notrelix.API.Contracts.Accounts.Requests;

public sealed record SaveSsoProviderRequest(string Name, string IssuerUrl, string ClientId, string? ClientSecret, bool JitProvisioning = true);

public sealed record AddSsoDomainRequest(string Domain);

public sealed record SetSsoRequiredRequest(bool Required);
