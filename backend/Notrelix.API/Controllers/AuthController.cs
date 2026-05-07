using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Commands.ForgotPassword;
using Notrelix.Application.Features.Identity.Commands.Login;
using Notrelix.Application.Features.Identity.Commands.Logout;
using Notrelix.Application.Features.Identity.Commands.RefreshToken;
using Notrelix.Application.Features.Identity.Commands.Register;
using Notrelix.Application.Features.Identity.Commands.ResetPassword;
using Notrelix.Application.Features.Identity.Queries.GetCurrentUser;
using Notrelix.Infrastructure.Jwt;

namespace Notrelix.API.Controllers;

public class AuthController : BaseController
{
    private readonly ICookieService _cookieService;
    public AuthController(ICookieService cookieService)
    {
        _cookieService = cookieService;
    }
    
    // POST api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterCommand command)
    {
        var result = await Sender.Send(command);
        return ToActionResult(result);
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginCommand command)
    {
        var result = await Sender.Send(command);
        if (result.Succeeded)
        {
            // Set cookie
            _cookieService.SetTokenCookie(result.Data.AccessToken, result.Data.RefreshToken);
        }
        return ToActionResult(result);
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await Sender.Send(command);
        return ToActionResult(result);
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromBody] LogoutCommand command)
    {
        var result = await Sender.Send(command);
        return ToActionResult(result);
    }

    // POST api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await Sender.Send(command);
        return ToActionResult(result);
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Sender.Send(command);
        return ToActionResult(result);
    }

    // GET api/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var query = new GetCurrentUserQuery { UserId = userId.Value };
        var result = await Sender.Send(query);
        return ToActionResult(result);
    }
}
