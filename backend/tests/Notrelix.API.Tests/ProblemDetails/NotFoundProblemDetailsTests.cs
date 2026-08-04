using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using Notrelix.Application.Common.Idempotency;
using DomainNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.API.Tests.ProblemDetails;

public class NotFoundProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _context;

    public NotFoundProblemDetailsTests()
    {
        _handler = new GlobalExceptionHandler(
            Mock.Of<ILogger<GlobalExceptionHandler>>(),
            Microsoft.Extensions.Options.Options.Create(new IdempotencyOptions()));
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
        _context.Request.Path = "/api/test";
        _context.TraceIdentifier = "test-trace-id";
    }

    [Fact]
    public async Task NotFoundException_WhenReturned_AllRequiredFieldsArePresent()
    {
        var exception = new DomainNotFoundException("Board", Guid.NewGuid());

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);

        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        json.TryGetProperty("type", out _).Should().BeTrue();
        json.TryGetProperty("title", out _).Should().BeTrue();
        json.TryGetProperty("detail", out _).Should().BeTrue();
        json.TryGetProperty("instance", out _).Should().BeTrue();
        json.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}
