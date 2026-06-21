using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;

namespace Notrelix.API.Tests.ProblemDetails;

public class UnexpectedExceptionProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _context;

    public UnexpectedExceptionProblemDetailsTests()
    {
        _handler = new GlobalExceptionHandler(Mock.Of<ILogger<GlobalExceptionHandler>>());
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
        _context.Request.Path = "/api/test";
        _context.TraceIdentifier = "test-trace-id";
    }

    [Fact]
    public async Task ProblemDetails_WhenAnyException_ReturnsTypeUri()
    {
        var exception = new InvalidOperationException("test");

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);
        var type = json.GetProperty("type").GetString()!;

        type.Should().StartWith("https://", "RFC 9457 requires type to be a URI");
    }

    [Fact]
    public async Task ProblemDetails_WhenUnhandledException_Returns500WithSafeDetail()
    {
        var exception = new InvalidOperationException("secret-internal-detail");

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);

        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status500InternalServerError);
        json.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task ProblemDetails_WhenAnyException_IncludesTraceId()
    {
        var exception = new Exception("test");

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);

        json.TryGetProperty("traceId", out _).Should().BeTrue("traceId is required for debugging");
        json.GetProperty("traceId").GetString().Should().Be("test-trace-id");
    }
}
