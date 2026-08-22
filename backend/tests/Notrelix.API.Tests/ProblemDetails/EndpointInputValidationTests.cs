using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Notrelix.API.Extensions;

namespace Notrelix.API.Tests.ProblemDetails;

public class EndpointInputValidationTests
{
    [Fact]
    public void InvalidInput_Returns400WithCanonicalValidationShape()
    {
        var http = (ProblemHttpResult)EndpointExtensions.InvalidInput("Invalid OAuth provider: unknown");

        http.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var pd = http.ProblemDetails.Should().BeAssignableTo<HttpValidationProblemDetails>().Subject;
        pd.Extensions["errorCode"].Should().Be("validation.failed");
        pd.Errors["_errors"].Should().ContainSingle().Which.Should().Be("Invalid OAuth provider: unknown");
    }

    [Fact]
    public void UnauthorizedProblem_Returns401WithUnauthorizedErrorCode()
    {
        var http = (ProblemHttpResult)EndpointExtensions.UnauthorizedProblem("Refresh token is missing.");

        http.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("auth.unauthorized");
        http.ProblemDetails.Detail.Should().Be("Refresh token is missing.");
    }
}
