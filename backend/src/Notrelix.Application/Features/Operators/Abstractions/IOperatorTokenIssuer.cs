using Notrelix.Domain.Operators;

namespace Notrelix.Application.Features.Operators.Abstractions;

public sealed record OperatorAccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>Issues the operator console's access token. It is signed with a key and audience no tenant token shares.</summary>
public interface IOperatorTokenIssuer
{
    OperatorAccessToken Issue(OperatorAccount operatorAccount, DateTimeOffset now);
}
