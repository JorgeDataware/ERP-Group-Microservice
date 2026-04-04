namespace GroupsMicroservice.Models.DB;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
    public ICollection<GroupMembers> GroupMembers { get; set; } = new List<GroupMembers>();
}