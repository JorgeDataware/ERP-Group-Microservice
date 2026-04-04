namespace GroupsMicroservice.Models.DB;

public class GroupMembers
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
}