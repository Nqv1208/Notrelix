namespace Notrelix.Application.Common.Security;

public static class OneTimeTokenProtectionPurposes
{
    public const string WorkspaceInvitation =
        "Notrelix.OneTimeTokenEnvelope.v1.WorkspaceInvitation";

    public const string EmailVerification =
        "Notrelix.OneTimeTokenEnvelope.v1.EmailVerification";

    public const string PasswordReset =
        "Notrelix.OneTimeTokenEnvelope.v1.PasswordReset";

    public const string ShareLink =
        "Notrelix.OneTimeTokenEnvelope.v1.ShareLink";
}
