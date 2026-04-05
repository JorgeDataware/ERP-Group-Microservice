using System.Net.NetworkInformation;

namespace GroupsMicroservice.Models.DB;


public enum Status
{
    Active = 1,
    Inactive = 2,
}
public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Status Status { get; set; }

    public ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
    public ICollection<GroupMembers> GroupMembers { get; set; } = new List<GroupMembers>();
}