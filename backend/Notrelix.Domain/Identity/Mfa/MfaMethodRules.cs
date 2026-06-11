using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Identity.Mfa;

public static class MfaMethodRules
{
    public static void EnsureValidCreation(MfaMethodType type, string? secretRef, string? destinationMasked)
    {
        if (type == MfaMethodType.AuthenticatorApp && string.IsNullOrWhiteSpace(secretRef))
        {
            throw new BusinessRuleException("Authenticator app MFA method requires a secret reference.");
        }

        if ((type == MfaMethodType.Email || type == MfaMethodType.Sms) && string.IsNullOrWhiteSpace(destinationMasked))
        {
            throw new BusinessRuleException("Email or SMS MFA method requires a masked destination.");
        }
    }
}
