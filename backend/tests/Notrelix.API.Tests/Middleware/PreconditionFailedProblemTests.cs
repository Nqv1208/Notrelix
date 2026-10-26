using Microsoft.AspNetCore.Http;
using Notrelix.API.ErrorHandling;
using Notrelix.Application.Common.Exceptions;

namespace Notrelix.API.Tests.Middleware;

public class PreconditionFailedProblemTests
{
    [Fact]
    public void AStalePrecondition_Answers412_AndCarriesTheCurrentStateInTheProblem()
    {
        var current = new { id = Guid.NewGuid(), version = 7 };
        var exception = new PreconditionFailedException("stale", "documents.block.version-conflict", 3, 7)
        {
            Extensions = new Dictionary<string, object?> { ["currentBlock"] = current },
        };

        var problem = ProblemDetailsMapper.Map(new DefaultHttpContext(), exception);

        problem.Status.Should().Be(StatusCodes.Status412PreconditionFailed);
        problem.Extensions["errorCode"].Should().Be("documents.block.version-conflict");
        problem.Extensions["currentBlock"].Should().BeSameAs(current);
    }

    [Fact]
    public void APreconditionWithoutExtensions_AddsNoExtraMembers()
    {
        var problem = ProblemDetailsMapper.Map(new DefaultHttpContext(), new PreconditionFailedException("stale"));

        problem.Status.Should().Be(StatusCodes.Status412PreconditionFailed);
        problem.Extensions.Keys.Should().BeEquivalentTo(["errorCode", "traceId"]);
    }
}
