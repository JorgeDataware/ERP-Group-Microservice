using GroupsMicroservice.Repositories.IRepositories;
using GroupsMicroservice.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GroupsMicroservice.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GroupController(IGroupRepositorie groupRepositorie, IAuthContextService authContextService) : ControllerBase
{
    private readonly IGroupRepositorie _groupRepositorie = groupRepositorie;
    private readonly IAuthContextService _authContextService = authContextService;

    private IActionResult UnauthorizedResponse(string message = "Invalid or missing user authentication.")
    {
        return Unauthorized(new
        {
            statusCode = 401,
            error = "Unauthorized",
            message
        });
    }

    private IActionResult ForbiddenResponse(string message = "You do not have permission to perform this action.")
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            statusCode = 403,
            error = "Forbidden",
            message
        });
    }

    [HttpGet("GetGroups")]
    [Authorize]
    public async Task<IActionResult> GetGroups()
    {
        var canSeeGroups = _authContextService.HasPermission("canRead_Groups");

        if (!canSeeGroups)
            return ForbiddenResponse();

        var groups = await _groupRepositorie.GetGroupsAsync();

        return Ok(groups.Value);
    }
}
