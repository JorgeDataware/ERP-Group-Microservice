using GroupsMicroservice.Models.Dto;
using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Repositories.IRepositories;

public interface IGroupRepositorie
{
    Task<Result<IEnumerable<GroupDto>>> GetGroupsAsync();
}
