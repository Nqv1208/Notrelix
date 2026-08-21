using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;

namespace Notrelix.API.Tests.ProblemDetails;

public class ResultErrorMappingTests
{
    private static ProblemHttpResult MapFailure(ApplicationError error)
    {
        var result = Result.Failure(error);
        return (ProblemHttpResult)result.ToApiResult();
    }

    [Fact]
    public void TypedValidationFailure_Returns400WithValidationErrorCode()
    {
        var http = MapFailure(new ApplicationError("identity.auth.weak-password", "Password too weak", ApplicationErrorType.Validation));

        http.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("validation.failed");
    }

    [Fact]
    public void TypedBusinessRuleFailure_Returns400WithBusinessRuleErrorCode()
    {
        var http = MapFailure(new ApplicationError("identity.auth.rate-limited", "Too many requests.", ApplicationErrorType.BusinessRule));

        http.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("business_rule.violation");
        http.ProblemDetails.Detail.Should().Be("Too many requests.");
    }

    [Fact]
    public void TypedAuthenticationFailure_Returns401WithUnauthorizedErrorCode()
    {
        var http = MapFailure(new ApplicationError("identity.auth.invalid-credentials", "Invalid email or password", ApplicationErrorType.Authentication));

        http.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("auth.unauthorized");
        http.ProblemDetails.Detail.Should().Be("Invalid email or password");
    }

    [Fact]
    public void TypedNotFoundFailure_Returns404WithResourceNotFoundErrorCode()
    {
        var http = MapFailure(new ApplicationError("identity.auth.user-not-found", "User not found.", ApplicationErrorType.NotFound));

        http.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("resource.not_found");
    }

    [Fact]
    public void TypedConflictFailure_Returns409WithConflictErrorCode()
    {
        var http = MapFailure(new ApplicationError("identity.auth.state-conflict", "Conflicting state.", ApplicationErrorType.Conflict));

        http.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("concurrency.conflict");
    }

    [Fact]
    public void TypedPreconditionFailure_Returns412WithErrorCodeAsErrorCode()
    {
        var http = MapFailure(new ApplicationError("identity.security.step-up-required", "Strong verification is required.", ApplicationErrorType.PreconditionFailed));

        http.StatusCode.Should().Be(StatusCodes.Status412PreconditionFailed);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("identity.security.step-up-required");
    }

    [Fact]
    public void LegacyStringFailure_KeepsValidationFailedShape()
    {
        var result = Result.Failure("Something failed");

        var http = (ProblemHttpResult)result.ToApiResult();

        http.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        http.ProblemDetails.Should().BeAssignableTo<HttpValidationProblemDetails>();
        http.ProblemDetails.Extensions["errorCode"].Should().Be("validation.failed");
    }

    [Fact]
    public void GenericResultTypedAuthenticationFailure_Returns401()
    {
        var result = Result<string>.Failure(
            new ApplicationError("identity.auth.invalid-refresh-token", "Refresh token is invalid or expired", ApplicationErrorType.Authentication));

        var http = (ProblemHttpResult)result.ToApiResult();

        http.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        http.ProblemDetails.Extensions["errorCode"].Should().Be("auth.unauthorized");
    }
}
