namespace Notrelix.Domain.Identity;

/// <summary>
/// Rule codes for the Identity bounded context.
/// </summary>
public static class IdentityRuleCodes
{
    // ── Users ─────────────────────────────────────────────────────────────
    public const string Identity_User_OAuthProviderAlreadyLinked = "Identity_User_OAuthProviderAlreadyLinked";
    public const string Identity_User_OAuthProviderMismatch = "Identity_User_OAuthProviderMismatch";
    public const string Identity_User_NoOAuthAccountForProvider = "Identity_User_NoOAuthAccountForProvider";
    public const string Identity_User_LastPrimaryAuthMethod = "Identity_User_LastPrimaryAuthMethod";
    public const string Identity_Login_TimeCannotMoveBackwards = "Identity_Login_TimeCannotMoveBackwards";

    // ── OAuth Profile Snapshot ────────────────────────────────────────────
    public const string Identity_OAuthProfileSnapshot_ProviderRequired = "Identity_OAuthProfileSnapshot_ProviderRequired";
    public const string Identity_OAuthProfileSnapshot_SchemaVersionMustBePositive = "Identity_OAuthProfileSnapshot_SchemaVersionMustBePositive";
    public const string Identity_OAuthProfileSnapshot_DataMustBeJsonObject = "Identity_OAuthProfileSnapshot_DataMustBeJsonObject";

    // ── Sessions ──────────────────────────────────────────────────────────
    public const string Identity_Session_ExpirationMustBeAfterCreation = "Identity_Session_ExpirationMustBeAfterCreation";
    public const string Identity_Session_CannotUpdateRefreshTokenOfInactive = "Identity_Session_CannotUpdateRefreshTokenOfInactive";
    public const string Identity_Session_CannotRevokeExpired = "Identity_Session_CannotRevokeExpired";
    public const string Identity_Session_CannotExpireRevoked = "Identity_Session_CannotExpireRevoked";

    // ── Tokens ────────────────────────────────────────────────────────────
    public const string Identity_ApiToken_InvalidScopesFormat = "Identity_ApiToken_InvalidScopesFormat";
    public const string Identity_ApiToken_CannotUseExpired = "Identity_ApiToken_CannotUseExpired";
    public const string Identity_ApiToken_CannotUseInactive = "Identity_ApiToken_CannotUseInactive";
    public const string Identity_OneTimeToken_HashVersionMustBePositive = "Identity_OneTimeToken_HashVersionMustBePositive";
    public const string Identity_OneTimeToken_ExpirationMustBeAfterCreation = "Identity_OneTimeToken_ExpirationMustBeAfterCreation";
    public const string Identity_OneTimeToken_AlreadyUsed = "Identity_OneTimeToken_AlreadyUsed";
    public const string Identity_OneTimeToken_CannotUseExpired = "Identity_OneTimeToken_CannotUseExpired";
    public const string Identity_OneTimeToken_CannotExpireUsed = "Identity_OneTimeToken_CannotExpireUsed";

    // ── Security ──────────────────────────────────────────────────────────
    public const string Identity_LoginAttempt_MustHaveUserIdOrEmail = "Identity_LoginAttempt_MustHaveUserIdOrEmail";
    public const string Identity_LoginAttempt_SuccessfulCannotHaveReason = "Identity_LoginAttempt_SuccessfulCannotHaveReason";
    public const string Identity_LoginAttempt_FailedMustHaveReason = "Identity_LoginAttempt_FailedMustHaveReason";

    // ── MFA ───────────────────────────────────────────────────────────────
    public const string Identity_Mfa_AuthenticatorRequiresSecret = "Identity_Mfa_AuthenticatorRequiresSecret";
    public const string Identity_Mfa_EmailSmsRequiresDestination = "Identity_Mfa_EmailSmsRequiresDestination";
    public const string Identity_Mfa_CannotVerifyDisabled = "Identity_Mfa_CannotVerifyDisabled";
    public const string Identity_Mfa_CannotSetPrimaryUnlessVerifiedActive = "Identity_Mfa_CannotSetPrimaryUnlessVerifiedActive";

    // ── Profiles ──────────────────────────────────────────────────────────
    public const string Identity_Profile_InvalidPreferencesJson = "Identity_Profile_InvalidPreferencesJson";
    public const string Identity_Profile_InvalidTheme = "Identity_Profile_InvalidTheme";
}
