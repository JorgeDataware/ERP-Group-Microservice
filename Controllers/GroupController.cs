using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using GroupsMicroservice.Models.Response;
using GroupsMicroservice.Repositories.IRepositories;
using GroupsMicroservice.Services.IServices;
using GroupsMicroservice.Utilities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroupsMicroservice.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GroupController(IGroupRepositorie groupRepositorie, IAuthContextService authContextService) : ControllerBase
{
    private readonly IGroupRepositorie _groupRepositorie = groupRepositorie;
    private readonly IAuthContextService _authContextService = authContextService;

    // Helper methods for standardized responses
    private IActionResult StandardSuccess<T>(int statusCode, string intOpCode, string message, T[] data)
    {
        var response = new StandardResponse<T>
        {
            StatusCode = statusCode,
            IntOpCode = intOpCode,
            Message = message,
            Data = data
        };
        return StatusCode(statusCode, response);
    }

    private IActionResult StandardError(int statusCode, string intOpCode, string message)
    {
        var response = new StandardResponse<object>
        {
            StatusCode = statusCode,
            IntOpCode = intOpCode,
            Message = message,
            Data = Array.Empty<object>()
        };
        return StatusCode(statusCode, response);
    }

    private IActionResult UnauthorizedResponse(string? message = null)
    {
        return StandardError(401, "EGRAU401", message ?? "Invalid or missing user authentication.");
    }

    private IActionResult ForbiddenResponse(string? message = null)
    {
        return StandardError(403, "EGRFB403", message ?? "You do not have permission to perform this action.");
    }

    private IActionResult NotFoundResponse(string message)
    {
        return StandardError(404, "EGRNF404", message);
    }

    private IActionResult ConflictResponse(string message)
    {
        return StandardError(409, "EGRCF409", message);
    }

    private IActionResult BadRequestResponse(string message)
    {
        return StandardError(400, "EGRBR400", message);
    }

    private IActionResult InternalServerErrorResponse(string message)
    {
        return StandardError(500, "EGRIN500", message);
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/
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

        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "UserNotFound" => NotFoundResponse(result.error.Message),
                "GroupAlreadyExists" => ConflictResponse(result.error.Message),
                _ => InternalServerErrorResponse("An unexpected error occurred while adding the group.")
            };
        }

        return StandardSuccess(201, "SGRCR201", "Group created successfully.", Array.Empty<object>());
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/members
    [HttpPost("AddMember")]
    [Authorize]
    public async Task<IActionResult> AddMembers([FromBody] AddMemberRequest request)
    {
        if (_authContextService.GetUserId() is not Guid requesterId)
            return UnauthorizedResponse();

        if (!_authContextService.HasPermission(GroupPermissions.CanUpdate))
            return ForbiddenResponse();

        var result = await _groupRepositorie.AddMemberAsync(request.GroupId, request.memberId, requesterId);
        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "GroupNotFoundOrInactive" => ConflictResponse(result.error.Message),
                "UserNotFound" => NotFoundResponse(result.error.Message),
                "MemberAlreadyExists" => ConflictResponse(result.error.Message),
                "OnlyOwnerCanAddMembers" => ForbiddenResponse("Only the group owner can add members to the group."),
                _ => InternalServerErrorResponse("An unexpected error occurred while adding the member to the group.")
            };
        }
        return StandardSuccess(200, "SGRMB200", "Group member added successfully.", Array.Empty<object>());
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/
    [HttpGet("GetGroups")]
    [Authorize]
    public async Task<IActionResult> GetGroups()
    {
        var canSeeGroups = _authContextService.HasPermission(GroupPermissions.CanRead);

        if (!canSeeGroups)
            return ForbiddenResponse();

        var groups = await _groupRepositorie.GetGroupsAsync();

        var groupsArray = groups.Value?.ToArray() ?? Array.Empty<GroupDto>();
        return StandardSuccess(200, "SGRRD200", "Groups retrieved successfully.", groupsArray);
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/{groupId}
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
                "GroupNotFound" => NotFoundResponse(result.error.Message),
                "UnactiveGroup" => ConflictResponse(result.error.Message),
                "OnlyOwnerCanEditGroup" => ForbiddenResponse("Only the group owner can edit the group."),
                _ => InternalServerErrorResponse("An unexpected error occurred while editing the group.")
            };
        }
        return StandardSuccess(200, "SGRUP200", "Group updated successfully.", Array.Empty<object>());
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/{groupId}
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
                "GroupNotFound" => NotFoundResponse(result.error.Message),
                _ => InternalServerErrorResponse("An unexpected error occurred while retrieving the group.")
            };
        }
        return StandardSuccess(200, "SGRRD200", "Group retrieved successfully.", new[] { result.Value });
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/{groupId}/members
    [HttpGet("GetGroupMembers/{groupId}")]
    [Authorize]
    public async Task<IActionResult> GetGroupMembers([FromRoute] Guid groupId)
    {
        var canSeeGroups = _authContextService.HasPermission(GroupPermissions.CanRead);
        if (!canSeeGroups)
            return ForbiddenResponse();
        
        var members = await _groupRepositorie.GetMembersAsync(groupId);

        if (!members.IsSuccess)
        {
            return members.error.Code switch
            {
                "GroupNotFound" => NotFoundResponse(members.error.Message),
                _ => InternalServerErrorResponse("An unexpected error occurred while retrieving the group members.")
            };
        }

        var membersArray = members.Value?.ToArray() ?? Array.Empty<GroupMemberDto>();
        return StandardSuccess(200, "SGRRD200", "Group members retrieved successfully.", membersArray);
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/{groupId}/members
    [HttpDelete("RemoveMember")]
    [Authorize]
    public async Task<IActionResult> RemoveMember([FromBody] RemoveMemberRequest request)
    {
        var canRemoveMembers = _authContextService.HasPermission(GroupPermissions.CanUpdate);
        if (!canRemoveMembers)
            return ForbiddenResponse();
        if (_authContextService.GetUserId() is not Guid requesterId)
            return UnauthorizedResponse();
        var result = await _groupRepositorie.RemoveMemberAsync(request.GroupId, request.memberId, requesterId);
        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "GroupNotFoundOrInactive" => ConflictResponse(result.error.Message),
                "MemberNotFound" => NotFoundResponse(result.error.Message),
                "OnlyOwnerCanRemoveMembers" => ForbiddenResponse("Only the group owner can remove members from the group."),
                _ => InternalServerErrorResponse("An unexpected error occurred while removing the member from the group.")
            };
        }
        return StandardSuccess(200, "SGRMB200", "Group member removed successfully.", Array.Empty<object>());
    }

    // Corresponde a: https://mi-gateway.onrender.com/groups/{groupId}/deactivate
    [HttpPatch("{groupId}/deactivate")]
    [Authorize]
    public async Task<IActionResult> DeactivateGroup([FromRoute] Guid groupId)
    {
        var canDeactivateGroup = _authContextService.HasPermission(GroupPermissions.CanDelete);
        if (!canDeactivateGroup)
            return ForbiddenResponse();

        if (_authContextService.GetUserId() is not Guid userId)
            return UnauthorizedResponse();

        var result = await _groupRepositorie.DeactivateGroupAsync(groupId, userId);
        if (!result.IsSuccess)
        {
            return result.error.Code switch
            {
                "GroupNotFound" => NotFoundResponse(result.error.Message),
                "OnlyOwnerCanDeactivateGroup" => ForbiddenResponse("Only the group owner can deactivate the group."),
                "UnactiveGroup" => ConflictResponse(result.error.Message),
                _ => InternalServerErrorResponse("An unexpected error occurred while deactivating the group.")
            };
        }
        return StandardSuccess(200, "SGRDL200", "Group deactivated successfully.", Array.Empty<object>());
    }
}
