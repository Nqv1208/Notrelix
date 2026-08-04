using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using Notrelix.Application.Common.Idempotency;
using DomainNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using DomainForbiddenException = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using AppValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.API.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionHandlerTests()
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
    public async Task IdempotencyIncompleteState_ShouldReturn503WithRetryAfter()
    {
        // Spec 3.8: committed active Processing is incomplete state — the API answers
        // 503 + Retry-After (from IdempotencyOptions) and never a replayed success.
        var exception = new IdempotencyIncompleteStateException("work-management.create-board-item.v1");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        pd.Extensions!["errorCode"].ToString().Should().Be("idempotency_state_incomplete");
        pd.Type.Should().Be("https://docs.notrelix.com/problems/idempotency_state_incomplete");

        _context.Response.Headers.RetryAfter.ToString()
            .Should().Be(((int)new IdempotencyOptions().IncompleteStateRetryAfter.TotalSeconds).ToString());
    }

    [Fact]
    public async Task ValidationException_ShouldReturn400WithErrorCodeAndErrors()
    {
        var exception = new ValidationException(new[]
        {
            new FluentValidation.Results.ValidationFailure("name", "Name is required"),
            new FluentValidation.Results.ValidationFailure("email", "Email is invalid"),
        });

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status400BadRequest);
        pd.Title.Should().Be("Validation failed");
        pd.Extensions!["errorCode"].ToString().Should().Be("validation.failed");
        pd.Extensions["traceId"].ToString().Should().Be("test-trace-id");

        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(JsonSerializer.Serialize(pd.Extensions["errors"]));
        errors.Should().ContainKey("name");
        errors.Should().ContainKey("email");
    }

    [Fact]
    public async Task AppValidationException_ShouldReturn400WithErrors()
    {
        var exception = new AppValidationException("Title is required");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status400BadRequest);
        pd.Extensions!["errorCode"].ToString().Should().Be("validation.failed");
    }

    [Fact]
    public async Task BusinessRuleViolationException_ShouldReturn400()
    {
        var exception = new DomainBusinessRuleException("board_archived", "Board is archived");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status400BadRequest);
        pd.Extensions!["errorCode"].ToString().Should().Be("business_rule.violation");
        pd.Detail.Should().Be("Board is archived");
    }

    [Fact]
    public async Task UnauthorizedException_ShouldReturn401()
    {
        var exception = new UnauthorizedException("Not authenticated");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Extensions!["errorCode"].ToString().Should().Be("auth.unauthorized");
    }

    [Fact]
    public async Task ForbiddenException_ShouldReturn403()
    {
        var exception = new Notrelix.Application.Common.Exceptions.ForbiddenException("Access denied");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status403Forbidden);
        pd.Extensions!["errorCode"].ToString().Should().Be("auth.forbidden");
    }

    [Fact]
    public async Task DomainForbiddenException_ShouldReturn403()
    {
        var exception = new DomainForbiddenException("No permission");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task NotFoundException_ShouldReturn404()
    {
        var exception = new DomainNotFoundException("Board", Guid.NewGuid());

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status404NotFound);
        pd.Extensions!["errorCode"].ToString().Should().Be("resource.not_found");
    }

    [Fact]
    public async Task ConflictException_ShouldReturn409()
    {
        var exception = new DomainConflictException("Board already exists");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status409Conflict);
        pd.Extensions!["errorCode"].ToString().Should().Be("concurrency.conflict");
    }

    [Fact]
    public async Task UnknownException_ShouldReturn500WithoutStackTrace()
    {
        var exception = new InvalidOperationException("Something broke");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Status.Should().Be(StatusCodes.Status500InternalServerError);
        pd.Extensions!["errorCode"].ToString().Should().Be("internal_server_error");
        pd.Detail.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task ProblemDetails_ShouldIncludeTraceId()
    {
        var exception = new Exception("test");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Extensions!["traceId"].ToString().Should().Be("test-trace-id");
    }

    [Fact]
    public async Task ProblemDetails_ShouldIncludeInstancePath()
    {
        var exception = new Exception("test");

        await _handler.TryHandleAsync(_context, exception, default);

        var pd = await ReadProblemDetailsAsync(_context);
        pd.Instance.Should().Be("/api/test");
    }

    private static async Task<Microsoft.AspNetCore.Mvc.ProblemDetails> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var result = await JsonSerializer.DeserializeAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return result!;
    }
}
