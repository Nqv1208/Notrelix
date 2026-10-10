using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using DomainConflictException = Notrelix.Application.Common.Exceptions.ConflictException;

namespace Notrelix.API.Tests.ProblemDetails;

public class ConflictProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _context;

    public ConflictProblemDetailsTests()
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
    public async Task ConflictException_WhenReturned_AllRequiredFieldsArePresent()
    {
        var exception = new DomainConflictException("Board already exists");

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);

        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        json.TryGetProperty("type", out _).Should().BeTrue();
        json.TryGetProperty("title", out _).Should().BeTrue();
        json.TryGetProperty("detail", out _).Should().BeTrue();
        json.TryGetProperty("instance", out _).Should().BeTrue();
        json.TryGetProperty("traceId", out _).Should().BeTrue();
        json.GetProperty("errorCode").GetString().Should().Be("concurrency.conflict");
    }

    [Fact]
    public async Task CodedConflictException_ReportsItsStableErrorCode()
    {
        var exception = new DomainConflictException("The declared neighbors are no longer adjacent.", "work.ordering.stale-placement");

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);

        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("errorCode").GetString().Should().Be("work.ordering.stale-placement");
    }

    /// <summary>WM-V3-API-001: the exact public error codes of the P3 write contract.</summary>
    [Theory]
    [InlineData("stale-placement")]
    [InlineData("ordering-conflict")]
    [InlineData("precondition")]
    public async Task P3WriteErrors_MapToTheirExactStatusAndCode(string kind)
    {
        Exception exception = kind switch
        {
            "stale-placement" => new DomainConflictException("The declared neighbors are no longer adjacent.", "work.ordering.stale-placement"),
            "ordering-conflict" => new DomainConflictException("The position was taken by a concurrent change.", "work.ordering.conflict"),
            _ => new Notrelix.Application.Common.Exceptions.PreconditionFailedException("The resource was modified by another request.", "common.precondition-failed"),
        };
        var (status, code) = kind switch
        {
            "stale-placement" => (StatusCodes.Status409Conflict, "work.ordering.stale-placement"),
            "ordering-conflict" => (StatusCodes.Status409Conflict, "work.ordering.conflict"),
            _ => (StatusCodes.Status412PreconditionFailed, "common.precondition-failed"),
        };

        await _handler.TryHandleAsync(_context, exception, default);

        _context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(_context.Response.Body);
        json.GetProperty("status").GetInt32().Should().Be(status);
        json.GetProperty("errorCode").GetString().Should().Be(code);
    }
}
