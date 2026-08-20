namespace Notrelix.Infrastructure.Auth.Credentials;

/// <summary>
/// Trusted credential claims emitted by authentication handlers after
/// successful verification. Credential-specific names avoid any ambiguity
/// with selected tenant context claims.
/// </summary>
public static class CredentialClaims
{
    public const string Kind = "notrelix:credential:kind";
    public const string ApiTokenId = "notrelix:credential:id";
    public const string AccountId = "notrelix:credential:account_id";
    public const string WorkspaceId = "notrelix:credential:workspace_id";

    public const string ApiTokenKindValue = "api_token";
}