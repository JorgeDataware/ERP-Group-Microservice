using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Repositories.IRepositories;

public interface IGroupRepositorie
{
    Task<Result<IEnumerable<GroupDto>>> GetGroupsAsync();
    Task<Result<Guid>> AddGroupAsync(Guid userId, AddGroupRequest request);
}
