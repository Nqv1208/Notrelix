namespace Notrelix.Application.Common.Exceptions;

public class SecurityMisconfigurationException : Exception
{
    public SecurityMisconfigurationException(string message)
        : base($"Security misconfiguration: {message}")
    {
    }

    public SecurityMisconfigurationException(string message, Exception inner)
        : base($"Security misconfiguration: {message}", inner)
    {
    }
}
