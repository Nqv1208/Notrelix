using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Commands.ForgotPassword;
using Notrelix.Application.Features.Identity.Commands.Login;
using Notrelix.Application.Features.Identity.Commands.Logout;
using Notrelix.Application.Features.Identity.Commands.RefreshToken;
using Notrelix.Application.Features.Identity.Commands.Register;
using Notrelix.Application.Features.Identity.Commands.ResetPassword;
using Notrelix.Application.Features.Identity.Queries.GetCurrentUser;

namespace Notrelix.API.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/auth")
            .WithTags("Auth")
            .WithOpenApi();

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Register a new account");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Login with email and password");

        group.MapPost("/refresh", RefreshToken)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithSummary("Refresh the access token");

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Logout and revoke refresh token");

        group.MapPost("/forgot-password", ForgotPassword)
            .AllowAnonymous()
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset email");

        group.MapPost("/reset-password", ResetPassword)
            .AllowAnonymous()
            .WithName("ResetPassword")
            .WithSummary("Reset password with token");

        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────

    private static async Task<IResult> Register(
        RegisterCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> Login(
        LoginCommand command,
        ISender sender,
        ICookieService cookieService)
    {
        var result = await sender.Send(command);

        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);
        }

        return result.ToApiResult();
    }

    private static async Task<IResult> RefreshToken(
        HttpContext httpContext,
        ISender sender,
        ICookieService cookieService)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Unauthorized();
        }

        var command = new RefreshTokenCommand { RefreshToken = refreshToken };
        var result = await sender.Send(command);
        if (result.Succeeded && result.Data is not null)
        {
            cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);
        }

        return result.ToApiResult();
    }

    private static async Task<IResult> Logout(
        LogoutCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> ForgotPassword(
        ForgotPasswordCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> ResetPassword(
        ResetPasswordCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> GetCurrentUser(
        ISender sender,
        ICurrentUser currentUser)
    {
        var query = new GetCurrentUserQuery { UserId = currentUser.UserId };
        var result = await sender.Send(query);
        return result.ToApiResult();
    }
}
