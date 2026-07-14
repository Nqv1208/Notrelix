namespace Notrelix.Application.Common.Tokens;

public interface IOneTimeTokenService
{
    IssuedOneTimeToken Generate(TokenPurpose purpose);
    ParsedOneTimeToken ParseAndHash(string presentedToken, TokenPurpose expectedPurpose);
}
