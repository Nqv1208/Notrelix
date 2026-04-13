using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Common.Models;
using System.IdentityModel.Tokens.Jwt;

namespace TodoApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    private ISender? _sender;
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    // Helper để convert Result thành ActionResult
    protected ActionResult ToActionResult(Result result)
    {
        if (result.Succeeded)
            return Ok();
        
        return BadRequest(new { errors = result.Errors });
    }

    protected ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        if (result.Succeeded)
            return Ok(result.Data);
        
        return BadRequest(new { errors = result.Errors });
    }

    // Lấy UserId từ JWT claims
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(claim =>
            string.Equals(claim.Type, JwtRegisteredClaimNames.Sub, StringComparison.OrdinalIgnoreCase)
            || string.Equals(claim.Type, "sub", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claim.Type, "userId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claim.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)
            || claim.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase));
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;
        
        return userId;
    }
}
