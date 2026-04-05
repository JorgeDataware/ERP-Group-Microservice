using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Repositories.IRepositories;

public interface IGroupRepositorie
{
    Task<Result<Guid>> AddGroupAsync(Guid userId, AddGroupRequest request);
    Task<Result<Guid>> AddMemberAsync(Guid groupId, Guid memberId, Guid requesterId);
    Task<Result<IEnumerable<GroupDto>>> GetGroupsAsync();

}
