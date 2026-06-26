namespace Notrelix.API.Options;

public sealed class ForwardedHeadersSettings
{
    public bool TrustAllInDevelopment { get; init; } = true;
    public bool RequireKnownProxyInProduction { get; init; } = true;
    public int ForwardLimit { get; init; } = 1;
    public List<string> KnownProxies { get; init; } = [];
    public List<string> KnownNetworks { get; init; } = [];
}
