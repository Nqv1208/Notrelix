using System.Text.Json;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Infrastructure.Integrations.Connections;

/// <summary>The `state` of the redirect flow, encrypted and signed with the key ring and valid for ten minutes.</summary>
internal sealed class IntegrationOAuthStateProtector : IIntegrationOAuthStateProtector
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly ITimeLimitedDataProtector _protector;

    public IntegrationOAuthStateProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("Notrelix.Integrations.OAuthState.v1").ToTimeLimitedDataProtector();

    public string Protect(IntegrationOAuthState state)
        => _protector.Protect(JsonSerializer.Serialize(state), Lifetime);

    public IntegrationOAuthState? Unprotect(string protectedState)
    {
        try
        {
            return JsonSerializer.Deserialize<IntegrationOAuthState>(_protector.Unprotect(protectedState));
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or JsonException or FormatException)
        {
            return null;
        }
    }
}
