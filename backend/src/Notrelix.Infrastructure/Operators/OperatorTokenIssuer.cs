using Notrelix.Application.Features.Operators.Abstractions;
using Notrelix.Domain.Operators;
using Notrelix.Infrastructure.Auth.Jwt;

namespace Notrelix.Infrastructure.Operators;

/// <summary>
/// Issues the operator console's access token (HS256). Its key, issuer and audience are the operator's own, so
/// the tenant bearer handler rejects it on signature and the operator handler rejects every tenant token the
/// same way. It carries the operator id and the <c>token_use=operator</c> marker, nothing about any tenant.
/// </summary>
internal sealed class OperatorTokenIssuer : IOperatorTokenIssuer
{
    private readonly OperatorOptions _options;
    private readonly SymmetricSecurityKey _key;

    public OperatorTokenIssuer(IOptions<OperatorOptions> options)
    {
        _options = options.Value;
        _key = new SymmetricSecurityKey(JwtKeyMaterial.DeriveKeyBytes(_options.Jwt.SigningKey));
    }

    public OperatorAccessToken Issue(OperatorAccount operatorAccount, DateTimeOffset now)
    {
        var expires = now.AddMinutes(_options.Jwt.ExpireMinutes);
        var token = new JwtSecurityToken(
            issuer: _options.Jwt.Issuer,
            audience: _options.Jwt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, operatorAccount.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(OperatorOptions.TokenUseClaim, OperatorOptions.TokenUseOperator),
            ],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        return new OperatorAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
