using Notrelix.API.Contracts.Identity;
using Notrelix.Application.Common.Models;

namespace Notrelix.API.Extensions;

public static class IdentityResultExtensions
{
    /// <summary>
    /// Maps an auth result to the token-free browser contract. A successful
    /// result that carries tokens must have had them written to cookies by the
    /// endpoint before calling this; the body never includes them.
    /// </summary>
    public static IResult ToSessionApiResult(this Result<AuthResult> result)
        => result.Succeeded && result.Data is not null
            ? Results.Ok(AuthSessionResponse.From(result.Data))
            : result.ToApiResult();

    /// <summary>
    /// Declares the problem responses of an Identity operation as
    /// <c>application/problem+json</c> so generated clients get typed errors.
    /// </summary>
    public static RouteHandlerBuilder ProducesProblems(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.ProducesProblem(statusCode);
        }

        return builder;
    }
}
