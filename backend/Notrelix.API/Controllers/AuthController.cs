using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Auth.Commands.ForgotPassword;
using Notrelix.Application.Features.Auth.Commands.Login;
using Notrelix.Application.Features.Auth.Commands.Logout;
using Notrelix.Application.Features.Auth.Commands.RefreshToken;
using Notrelix.Application.Features.Auth.Commands.Register;
using Notrelix.Application.Features.Auth.Commands.ResetPassword;
using Notrelix.Application.Features.Auth.Queries.GetCurrentUser;

namespace Notrelix.API.Controllers;

public class AuthController : BaseController
{
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
