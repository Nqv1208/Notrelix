using Notrelix.Application.Common.Oidc;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>A discovery client that answers per issuer from a table; an issuer nobody described is unreachable.</summary>
public sealed class FakeOidcDiscoveryClient : IOidcDiscoveryClient
{
    private readonly Dictionary<string, OidcDiscoveryResult> _answers = new(StringComparer.Ordinal);

    public int Calls { get; private set; }

    public FakeOidcDiscoveryClient Serve(string issuer, OidcDiscoveryDocument? document = null)
    {
        _answers[issuer] = OidcDiscoveryResult.Ok(document ?? Document(issuer));
        return this;
    }

    public FakeOidcDiscoveryClient Refuse(string issuer, string failure)
    {
        _answers[issuer] = OidcDiscoveryResult.Fail(failure);
        return this;
    }

    public static OidcDiscoveryDocument Document(string issuer) => new(
        issuer, issuer.TrimEnd('/') + "/authorize", issuer.TrimEnd('/') + "/token", issuer.TrimEnd('/') + "/jwks");

    public Task<OidcDiscoveryResult> DiscoverAsync(string issuer, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(_answers.TryGetValue(issuer, out var answer) ? answer : OidcDiscoveryResult.Fail(OidcDiscoveryFailure.Unreachable));
    }
}
