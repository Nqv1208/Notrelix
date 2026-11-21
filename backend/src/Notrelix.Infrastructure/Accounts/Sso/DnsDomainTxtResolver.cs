using DnsClient;
using Notrelix.Application.Features.Accounts.Abstractions;

namespace Notrelix.Infrastructure.Accounts.Sso;

/// <summary>
/// Reads the TXT records of a domain with DnsClient (the system's resolvers, 3 s timeout, no retry storm). Any error
/// (no such name, a timeout, an unreachable resolver) is the answer "no records": the domain is not verified yet.
/// </summary>
public sealed class DnsDomainTxtResolver : IDomainTxtResolver
{
    private readonly ILookupClient _client;
    private readonly ILogger<DnsDomainTxtResolver> _logger;

    public DnsDomainTxtResolver(ILogger<DnsDomainTxtResolver> logger)
    {
        _client = new LookupClient(new LookupClientOptions
        {
            Timeout = TimeSpan.FromSeconds(3),
            Retries = 1,
            ThrowDnsErrors = false,
            UseCache = false,
        });
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ResolveTxtAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            var response = await _client.QueryAsync(domain, QueryType.TXT, cancellationToken: timeout.Token);
            return response.Answers.TxtRecords().Select(r => string.Concat(r.Text)).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(ex, "TXT lookup for a claimed domain did not answer; treating it as no records.");
            return [];
        }
    }
}
