using GroupsMicroservice.Models.DB;

namespace GroupsMicroservice.Models.Dto;

public class GetCompleteGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public string Owner { get; set; } = null!;
    public Status Status { get; set; }
    public IEnumerable<GroupMemberDto> Members { get; set; } = new List<GroupMemberDto>();
}

public class GroupMemberDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string CompleteName { get; set; } = null!;
}
