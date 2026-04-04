using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Utilities;

public class GroupErrors
{
    public static Error GroupNotFound => new Error("GroupNotFound", "The specified group was not found.");
}
