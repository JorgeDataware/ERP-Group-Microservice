using AutoMapper;
using Dapper;
using GroupsMicroservice.Data;
using GroupsMicroservice.Models.DB;
using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using GroupsMicroservice.Repositories.IRepositories;
using GroupsMicroservice.Utilities;
using GroupsMicroservice.Utilities.Constants;
using Microsoft.EntityFrameworkCore;
using System.Data;
using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Repositories;

public class GroupRepositori(AppDbContext context, IDbConnection dbConnection, IMapper mapper) : IGroupRepositorie
{
    private readonly AppDbContext _context = context;
    private readonly IDbConnection _dbConnection = dbConnection;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<IEnumerable<GroupDto>>> GetGroupsAsync()
    {
        var sql = @"
            SELECT
                g.id,
                g.name,
                g.description,
                CONCAT_WS(' ', u.first_name, NULLIF(u.middle_name, ''), u.last_name) AS owner
            FROM
                ""group"" g
            JOIN ""user"" u ON g.created_by_user_id = u.id";

        var connection = _dbConnection;

        var groups = await _dbConnection.QueryAsync<GroupDto>(sql);

        return Result<IEnumerable<GroupDto>>.Success(groups);
    }

    public async Task<Result<Guid>> AddGroupAsync(Guid userId, AddGroupRequest request)
    {
        var groupExists = await _context.group
            .AnyAsync(g => g.Name == request.Name);

        if (groupExists)
            return Result<Guid>.Failure(GroupErrors.GroupAlreadyExists);

        var userExists = await _context.user
            .AnyAsync(u => u.Id == userId);

        if (!userExists)
            return Result<Guid>.Failure(GroupErrors.UserNotFound);

        var newGroup = _mapper.Map<Group>(request);

        newGroup.CreatedByUserId = userId;

        await _context.group.AddAsync(newGroup);

        await _context.SaveChangesAsync();

        return Result<Guid>.Success(newGroup.Id);
    }

    public async Task<Result<Guid>> AddMemberAsync(Guid groupId, Guid memberId, Guid requesterId)
    {
        var group = await _context.group
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Where(g => g.Status == Status.Active)
            .Select(g => new
            {
                g.Id,
                g.CreatedByUserId
            })
            .FirstOrDefaultAsync();

        if (group == null)
            return Result<Guid>.Failure(GroupErrors.GroupNotFoundOrInactive);

        if (group.CreatedByUserId != requesterId)
            return Result<Guid>.Failure(GroupErrors.OnlyOwnerCanAddMembers);

        var userExists = await _context.user
            .AnyAsync(u => u.Id == memberId);

        if (!userExists)
            return Result<Guid>.Failure(GroupErrors.UserNotFound);

        var memberExists = await _context.group_members
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == memberId);

        if (memberExists)
            return Result<Guid>.Failure(GroupErrors.MemberAlreadyExists);

        var newMember = new GroupMembers
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = memberId
        };

        await _context.group_members.AddAsync(newMember);
        await _context.SaveChangesAsync();

        return Result<Guid>.Success(groupId);
    }

    public async Task<Result<Guid>> EditGroupAsync(Guid groupId, Guid requesterId, EditGroupRequest request)
    {
        var groupToUpdate = await _context.group
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (groupToUpdate == null)
            return Result<Guid>.Failure(GroupErrors.GroupNotFoundOrInactive);

        if (groupToUpdate.CreatedByUserId != requesterId)
            return Result<Guid>.Failure(GroupErrors.OnlyOwnerCanEditGroup);

        _mapper.Map(request, groupToUpdate);

        await _context.SaveChangesAsync();

        return Result<Guid>.Success(groupId);
    }

    public async Task<Result<GetCompleteGroupDto>> GetGroupByIdAsync(Guid groupId)
    {
        const string groupSql = @"
        SELECT
            g.id,
            g.name,
            g.description,
            g.status,
            g.created_by_user_id AS created_by_user_id,
            CONCAT_WS(' ', u.first_name, NULLIF(u.middle_name, ''), u.last_name) AS owner
        FROM
            ""group"" g
        JOIN ""user"" u ON g.created_by_user_id = u.id
        WHERE g.id = @GroupId;";

        const string membersSql = @"
        SELECT
            u.id,
            u.user_name,
            CONCAT_WS(' ', u.first_name, NULLIF(u.middle_name, ''), u.last_name) AS complete_name
        FROM
            group_members gm
        JOIN ""user"" u ON gm.user_id = u.id
        JOIN ""group"" g ON gm.group_id = g.id
        WHERE g.id = @GroupId;";

        var parameters = new { GroupId = groupId };

        var group = await _dbConnection.QueryFirstOrDefaultAsync<GetCompleteGroupDto>(groupSql, parameters);

        if (group == null)
            return Result<GetCompleteGroupDto>.Failure(GroupErrors.GroupNotFoundOrInactive);

        var members = await _dbConnection.QueryAsync<GroupMemberDto>(membersSql, parameters);

        group.Members = members;

        return Result<GetCompleteGroupDto>.Success(group);
    }

    public async Task<Result<IEnumerable<GroupMemberDto>>> GetMembersAsync(Guid groupId)
    {
        var groupExists = await _context.group
            .AsNoTracking()
            .AnyAsync(g => g.Id == groupId);

        if (!groupExists)
            return Result<IEnumerable<GroupMemberDto>>.Failure(GroupErrors.GroupNotFoundOrInactive);

        const string membersSql = @"
            SELECT
                u.id,
                u.user_name,
                CONCAT_WS(' ', u.first_name, NULLIF(u.middle_name, ''), u.last_name) AS complete_name
            FROM
                group_members gm
            JOIN ""user"" u ON gm.user_id = u.id
            JOIN ""group"" g ON gm.group_id = g.id
            WHERE g.id = @GroupId;";

        var parameters = new { GroupId = groupId };

        var members = await _dbConnection.QueryAsync<GroupMemberDto>(membersSql, parameters);

        return Result<IEnumerable<GroupMemberDto>>.Success(members);
    }

    public async Task<Result<Guid>> RemoveMemberAsync(Guid groupId, Guid memberId, Guid requesterId)
    {
        var groupOwnerId = await _context.group
            .AsNoTracking()
            .Where(g => g.Id == groupId && g.Status == Status.Active)
            .Select(g => (Guid?)g.CreatedByUserId)
            .FirstOrDefaultAsync();

        if (groupOwnerId == null)
            return Result<Guid>.Failure(GroupErrors.GroupNotFoundOrInactive);

        if (groupOwnerId != requesterId)
            return Result<Guid>.Failure(GroupErrors.OnlyOwnerCanRemoveMembers);

        var affectedRows = await _context.group_members
            .Where(gm => gm.GroupId == groupId && gm.UserId == memberId)
            .ExecuteDeleteAsync();

        if (affectedRows == 0)
            return Result<Guid>.Failure(GroupErrors.MemberNotFound);

        return Result<Guid>.Success(groupId);
    }

    public async Task<Result<Guid>> DeactivateGroupAsync(Guid groupId, Guid requesterId)
    {
        var groupToUpdate = await _context.group
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (groupToUpdate == null)
            return Result<Guid>.Failure(GroupErrors.GroupNotFoundOrInactive);

        if (groupToUpdate.CreatedByUserId != requesterId && SystemUsers.SuperAdminId != requesterId)
            return Result<Guid>.Failure(GroupErrors.OnlyOwnerCanDeactivateGroup);

        if (groupToUpdate.Status == Status.Inactive)
            return Result<Guid>.Failure(GroupErrors.UnactiveGroup);

        groupToUpdate.Status = Status.Inactive;

        await _context.SaveChangesAsync();

        return Result<Guid>.Success(groupId);
    }
}
