using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;

namespace Notrelix.API.Tests.ProblemDetails;

public class ValidationProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _context;

    public ValidationProblemDetailsTests()
    {
        _handler = new GlobalExceptionHandler(Mock.Of<ILogger<GlobalExceptionHandler>>());
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
        _context.Request.Path = "/api/test";
        _context.TraceIdentifier = "test-trace-id";
    }

    [Fact]
    public async Task ValidationException_WhenReturned_AllRequiredFieldsArePresent()
    {
        var exception = new ValidationException(new[]
        {
            new FluentValidation.Results.ValidationFailure("name", "Name is required"),
            new FluentValidation.Results.ValidationFailure("email", "Email is invalid"),
        });

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);

        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status400BadRequest);
        json.GetProperty("title").GetString().Should().Be("Validation failed");
        json.TryGetProperty("type", out _).Should().BeTrue();
        json.TryGetProperty("detail", out _).Should().BeTrue();
        json.TryGetProperty("instance", out _).Should().BeTrue();
        json.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}
