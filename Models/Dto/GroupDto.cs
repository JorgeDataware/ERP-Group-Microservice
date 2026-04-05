using GroupsMicroservice.Models.DB;

namespace GroupsMicroservice.Models.Dto;

public class GroupDto
{
    public Guid id { get; set; }
    public string name { get; set; } = null!;
    public string description { get; set; } = null!;
    public string owner { get; set; } = null!;
    public Status status { get; set; }
}
