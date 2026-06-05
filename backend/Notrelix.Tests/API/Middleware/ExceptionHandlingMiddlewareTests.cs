using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.Middleware;

namespace Notrelix.API.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionIsValidationError_ShouldReturnBadRequest()
    {
        var middleware = CreateMiddleware(new ArgumentException("Name is required"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var response = await ReadResponseAsync(context);
        response.Type.Should().Be("ValidationError");
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionIsMediatRNotificationContractError_ShouldReturnInternalServerError()
    {
        var middleware = CreateMiddleware(new ArgumentException("notification does not implement INotification", "notification"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var response = await ReadResponseAsync(context);
        response.Type.Should().Be("InternalServerError");
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionIsMediatRInternalNotificationContractError_ShouldReturnInternalServerError()
    {
        var middleware = CreateMiddleware(new ArgumentException("notification does not implement $INotification", "notification"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var response = await ReadResponseAsync(context);
        response.Type.Should().Be("InternalServerError");
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(Exception exception)
    {
        return new ExceptionHandlingMiddleware(
            _ => throw exception,
            Mock.Of<ILogger<ExceptionHandlingMiddleware>>());
    }

    private static async Task<ErrorResponseDto> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ErrorResponseDto>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return response!;
    }

    private sealed record ErrorResponseDto(string Type, string Message, Dictionary<string, string[]>? Errors);
}
