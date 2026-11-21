namespace Notrelix.Application.Features.Accounts.Public.Status;

/// <summary>
/// Accounts-published fact: is an Account operating. Other modules read the status only through this port, never
/// through the Accounts persistence. A <see cref="AccountStatusFact.Suspended"/> or
/// <see cref="AccountStatusFact.Closed"/> Account is read-only end to end (owner decision 7, lead decision 12):
/// automation runs, outbound webhooks and the anonymous writes of public forms and share links are refused or
/// skipped with the reason <see cref="AccountStatusReasons.Suspended"/>; reads stay open and nothing is deleted.
/// </summary>
public interface IAccountStatusFacts
{
    /// <summary>The status of an Account. An Account that cannot be found is reported as Closed (the stricter reading).</summary>
    Task<AccountStatusFact> GetAsync(Guid accountId, CancellationToken cancellationToken);
}

/// <summary>Trialing counts as Active; Closed is treated like Suspended wherever the fact is applied.</summary>
public enum AccountStatusFact
{
    Active,
    Suspended,
    Closed,
}

public static class AccountStatusFactExtensions
{
    /// <summary>True when the Account may run automation and serve anonymous writes.</summary>
    public static bool IsOperating(this AccountStatusFact status) => status == AccountStatusFact.Active;
}

public static class AccountStatusReasons
{
    /// <summary>The stable code recorded when work is refused or skipped because the Account is suspended or closed.</summary>
    public const string Suspended = "account.suspended";
}
