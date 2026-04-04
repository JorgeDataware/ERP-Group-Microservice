namespace GroupsMicroservice.Models.DB;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<GroupMembers> GroupMembers { get; set; } = new List<GroupMembers>();
}