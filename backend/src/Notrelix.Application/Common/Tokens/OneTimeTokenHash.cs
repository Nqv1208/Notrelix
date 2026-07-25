namespace Notrelix.Application.Common.Tokens;

public sealed record OneTimeTokenHash(string Value, int HashVersion)
{
    public int Version => HashVersion;
}
