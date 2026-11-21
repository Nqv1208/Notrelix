namespace Notrelix.Application.Features.Accounts.Security;

/// <summary>Keys of the security policy facts kept in <c>account.account_settings</c>. The value is "true" or "false".</summary>
public static class AccountSecuritySettingKeys
{
    public const string RequireMfa = "security.require-mfa";
    public const string True = "true";
    public const string False = "false";
}
