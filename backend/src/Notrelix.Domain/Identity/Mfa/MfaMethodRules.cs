namespace Notrelix.Domain.Identity.Mfa;

public static class MfaMethodRules
{
    public static void EnsureValidCreation(MfaMethodType type, SecretRef? secretRef, string? destinationMasked)
    {
        if (type == MfaMethodType.AuthenticatorApp && secretRef is null)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Mfa_AuthenticatorRequiresSecret, "Authenticator app MFA method requires a secret reference.");
        }

        if ((type == MfaMethodType.Email || type == MfaMethodType.Sms) && string.IsNullOrWhiteSpace(destinationMasked))
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Mfa_EmailSmsRequiresDestination, "Email or SMS MFA method requires a masked destination.");
        }
    }
}
