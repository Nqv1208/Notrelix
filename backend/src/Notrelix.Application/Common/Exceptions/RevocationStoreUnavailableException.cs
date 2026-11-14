namespace Notrelix.Application.Common.Exceptions;

/// <summary>
/// The token revocation store could not be read, so the request cannot be shown to carry a live
/// credential. The request is refused (fail closed) with a retryable 503, never let through.
/// </summary>
public sealed class RevocationStoreUnavailableException : Exception
{
    public RevocationStoreUnavailableException(Exception inner)
        : base("Token revocation could not be checked.", inner)
    {
    }
}
