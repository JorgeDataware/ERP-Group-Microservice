using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Repositories.IRepositories;

public interface IGroupRepositorie
{
    Task<Result<Guid>> AddGroupAsync(Guid userId, AddGroupRequest request);
    Task<Result<Guid>> AddMembersAsync(Guid groupId, IEnumerable<Guid> memberIds);
    Task<Result<IEnumerable<GroupDto>>> GetGroupsAsync();

}
