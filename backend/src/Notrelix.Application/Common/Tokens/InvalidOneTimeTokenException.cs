namespace Notrelix.Application.Common.Tokens;

public sealed class InvalidOneTimeTokenException : FormatException
{
    public InvalidOneTimeTokenException()
        : base("The token is invalid or expired.")
    {
    }
}
