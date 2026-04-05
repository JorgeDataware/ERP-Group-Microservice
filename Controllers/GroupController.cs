using AutoMapper.Execution;
using GroupsMicroservice.Models.Request;
using GroupsMicroservice.Repositories;
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

    [HttpPost("AddMember")]
    [Authorize]
    public async Task<IActionResult> AddMembers([FromBody] AddMemberRequest request)
    {
        if (_authContextService.GetUserId() is not Guid requesterId)
            return UnauthorizedResponse();

        if (!_authContextService.HasPermission(GroupPermissions.CanUpdate))
            return ForbiddenResponse();

        var result = await _groupRepositorie.AddMemberAsync(request.GroupId, request.UserId, requesterId);
        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "GroupNotFound" => NotFound(new
                {
                    statusCode = 404,
                    error = result.error.Code,
                    message = result.error.Message
                }),
                "UserNotFound" => NotFound(new
                {
                    statusCode = 404,
                    error = result.error.Code,
                    message = result.error.Message
                }),
                "MemberAlreadyExists" => Conflict(new
                {
                    statusCode = 409,
                    error = result.error.Code,
                    message = result.error.Message
                }),
                "OnlyOwnerCanAddMembers" => ForbiddenResponse("Only the group owner can add members to the group."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = 500,
                    error = "InternalServerError",
                    message = "An unexpected error occurred while adding the member to the group."
                })
            };
        }
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

    [HttpPatch("EditGroup/{groupId}")]
    [Authorize]
    public async Task<IActionResult> EditGroup([FromRoute] Guid groupId, [FromBody] EditGroupRequest request)
    {
        var canEditGroup = _authContextService.HasPermission(GroupPermissions.CanUpdate);

        if (!canEditGroup)
            return ForbiddenResponse();

        if (_authContextService.GetUserId() is not Guid userId)
            return UnauthorizedResponse();

        var result = await _groupRepositorie.EditGroupAsync(groupId, userId, request);

        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "GroupNotFound" => NotFound(new
                {
                    statusCode = 404,
                    error = result.error.Code,
                    message = result.error.Message
                }),
                "OnlyOwnerCanEditGroup" => ForbiddenResponse("Only the group owner can edit the group."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = 500,
                    error = "InternalServerError",
                    message = "An unexpected error occurred while editing the group."
                })
            };
        }
        return NoContent();
    }

    [HttpGet("GetGroupById/{groupId}")]
    [Authorize]
    public async Task<IActionResult> GetGroupById([FromRoute] Guid groupId)
    {
        var canSeeGroups = _authContextService.HasPermission(GroupPermissions.CanRead);
        if (!canSeeGroups)
            return ForbiddenResponse();
        var result = await _groupRepositorie.GetGroupByIdAsync(groupId);
        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "GroupNotFound" => NotFound(new
                {
                    statusCode = 404,
                    error = result.error.Code,
                    message = result.error.Message
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = 500,
                    error = "InternalServerError",
                    message = "An unexpected error occurred while retrieving the group."
                })
            };
        }
        return Ok(result.Value);
    }
}
