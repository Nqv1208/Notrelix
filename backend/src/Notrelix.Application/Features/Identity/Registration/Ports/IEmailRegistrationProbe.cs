namespace Notrelix.Application.Features.Identity.Registration.Ports;

/// <summary>
/// Identity-owned probe: whether an identity already exists for a normalized address. The answer is a
/// boolean only; no user row is read by the caller (a registration or an invitation preview runs before the
/// caller may see any user row). The unique index on the address still decides a registration race.
/// </summary>
public interface IEmailRegistrationProbe
{
    Task<bool> IsRegisteredAsync(string normalizedEmail, CancellationToken cancellationToken);
}
