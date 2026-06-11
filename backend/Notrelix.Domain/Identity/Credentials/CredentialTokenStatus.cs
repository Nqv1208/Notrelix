using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Credentials;

public enum CredentialTokenStatus
{
    Active,
    Consumed,
    Expired
}
