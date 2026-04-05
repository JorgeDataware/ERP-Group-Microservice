using AutoMapper;
using Dapper;
using GroupsMicroservice.Data;
using GroupsMicroservice.Models.DB;
using GroupsMicroservice.Models.Dto;
using GroupsMicroservice.Models.Request;
using GroupsMicroservice.Repositories.IRepositories;
using GroupsMicroservice.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection.Metadata.Ecma335;
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
        var newGroup = _mapper.Map<Group>(request);

        Console.WriteLine("Id del usuario que crea el grupo: " + userId);

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
            .Select(g => new
            {
                g.Id,
                g.CreatedByUserId
            })
            .FirstOrDefaultAsync();

        if (group == null)
            return Result<Guid>.Failure(GroupErrors.GroupNotFound);

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
}
