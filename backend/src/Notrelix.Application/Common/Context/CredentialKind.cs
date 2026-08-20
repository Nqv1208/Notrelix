namespace Notrelix.Application.Common.Context;

/// <summary>
/// The kind of credential that authenticated the current request.
/// Distinct from the resolved tenant/account context: a credential is bound
/// to a scope at issuance, while <see cref="ICurrentTenantContext"/> reflects
/// the workspace selected for the current request.
/// </summary>
public enum CredentialKind
{
    /// <summary>No request-scoped credential context (anonymous or not established).</summary>
    None,

    /// <summary>A browser/session credential (JWT bearer or cookie session).</summary>
    UserSession,

    /// <summary>A workspace-scoped API token credential.</summary>
    ApiToken
}