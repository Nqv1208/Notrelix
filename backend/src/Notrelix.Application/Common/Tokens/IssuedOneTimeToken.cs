namespace Notrelix.Application.Common.Tokens;

public sealed record IssuedOneTimeToken(
    string RawToken,
    string TokenHash,
    int HashVersion);
