namespace Notrelix.Application.Common.Context;

/// <summary>
/// Describes the credential that authenticated the current request.
/// Implementations must source this from trusted authentication claims only;
/// Application never sees claims, HTTP, schemes, or raw tokens.
/// </summary>
public interface ICurrentCredentialContext
{
    /// <summary>The kind of credential that authenticated the request.</summary>
    CredentialKind Kind { get; }

    /// <summary>The API token id, when the credential is an API token.</summary>
    Guid? ApiTokenId { get; }

    /// <summary>The account the API token was issued for, when applicable.</summary>
    Guid? BoundAccountId { get; }

    /// <summary>The workspace the API token was issued for, when applicable.</summary>
    Guid? BoundWorkspaceId { get; }
}