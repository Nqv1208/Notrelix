namespace Notrelix.Application.Features.Accounts.Abstractions;

/// <summary>
/// Looks up the DNS TXT records of a domain to prove its ownership. Any failure (no such name, a timeout, an unreachable
/// resolver) is the same answer as no records: a domain that cannot be read is simply not verified yet.
/// </summary>
public interface IDomainTxtResolver
{
    Task<IReadOnlyList<string>> ResolveTxtAsync(string domain, CancellationToken cancellationToken);
}
