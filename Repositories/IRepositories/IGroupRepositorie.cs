using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Repositories.IRepositories;

public interface IGroupRepositorie
{
    Task<Result<Guid>> AddGroupAsync(Guid userId, AddGroupRequest request);
    Task<Result<Guid>> AddMemberAsync(Guid groupId, Guid memberId, Guid requesterId);
    Task<Result<IEnumerable<GroupDto>>> GetGroupsAsync();
    Task<Result<Guid>> EditGroupAsync(Guid groupId, Guid requesterId, EditGroupRequest request);
    Task<Result<GetCompleteGroupDto>> GetGroupByIdAsync(Guid groupId);
    Task<Result<IEnumerable<GroupMemberDto>>> GetMembersAsync(Guid groupId);
    Task<Result<Guid>> RemoveMemberAsync(Guid groupId, Guid memberId, Guid requesterId);
    Task<Result<Guid>> DeactivateGroupAsync(Guid groupId, Guid requesterId);
}
