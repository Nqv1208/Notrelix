using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Auth.Commands.UpdateProfile;

namespace Notrelix.API.Controllers;

[ApiController]
public class UsersController : BaseController
{
    // PATCH api/users/profile
    [Authorize]
    [HttpPatch("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var request = command with { UserId = userId.Value };

        var result = await Sender.Send(request);
        return ToActionResult(result);
    }
}

