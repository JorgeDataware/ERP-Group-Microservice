using GroupsMicroservice.Models.Request;
using GroupsMicroservice.Repositories.IRepositories;
using GroupsMicroservice.Services.IServices;
using GroupsMicroservice.Utilities.Constants;
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

    [HttpPost("AddGroup")]
    [Authorize]
    public async Task<IActionResult> AddGroup([FromBody] AddGroupRequest request)
    {
        var canCreateGroup = _authContextService.HasPermission(GroupPermissions.CanCreate);

        if (!canCreateGroup)
            return ForbiddenResponse();


        if (_authContextService.GetUserId() is not Guid userId)
            return UnauthorizedResponse("User authentication is required to add a group.");

        var result = await _groupRepositorie.AddGroupAsync(userId, request);

        return NoContent();
    }

    [HttpPost("AddMembers/{groupId}")]
    [Authorize]
    public async Task<IActionResult> AddMembers([FromRoute] Guid groupId, [FromBody] IEnumerable<Guid> memberIds)
    {
        var canAddMembers = _authContextService.HasPermission(GroupPermissions.CanUpdate);
        if (!canAddMembers)
            return ForbiddenResponse();
        var result = await _groupRepositorie.AddMembersAsync(groupId, memberIds);
        if (!result.IsSuccess)
            return NotFound(new
            {
                statusCode = 404,
                error = result.error.Code,
                message = result.error.Message
            });
        return NoContent();
    }

    [HttpGet("GetGroups")]
    [Authorize]
    public async Task<IActionResult> GetGroups()
    {
        var canSeeGroups = _authContextService.HasPermission(GroupPermissions.CanRead);

        if (!canSeeGroups)
            return ForbiddenResponse();

        var groups = await _groupRepositorie.GetGroupsAsync();

        return Ok(groups.Value);
    }
}
