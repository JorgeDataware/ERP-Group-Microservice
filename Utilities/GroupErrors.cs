using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Utilities;

public class GroupErrors
{
    public static Error GroupNotFound => new Error("GroupNotFound", "The specified group was not found.");
    public static Error MemberAlreadyExists => new Error("MemberAlreadyExists", "The specified member is already part of the group.");
    public static Error UserNotFound => new Error("UserNotFound", "The specified user was not found.");
    public static Error OnlyOwnerCanAddMembers => new Error("OnlyOwnerCanAddMembers", "Only the group owner can add members to the group.");
}
