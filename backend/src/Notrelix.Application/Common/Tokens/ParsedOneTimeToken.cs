namespace Notrelix.Application.Common.Tokens;

public sealed record ParsedOneTimeToken(
    string TokenHash,
    int HashVersion);
