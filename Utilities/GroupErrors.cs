using UsersMicroservice.Utilities.Abstractions;

namespace GroupsMicroservice.Utilities;

public class GroupErrors
{
    public static Error GroupNotFoundOrInactive => new Error("GroupNotFoundOrInactive", "The specified group was not found or is inactive.");
    public static Error GroupNotFound => new Error("GroupNotFound", "The specified group was not found.");
    public static Error MemberAlreadyExists => new Error("MemberAlreadyExists", "The specified member is already part of the group.");
    public static Error UserNotFound => new Error("UserNotFound", "The specified user was not found.");
    public static Error OnlyOwnerCanAddMembers => new Error("OnlyOwnerCanAddMembers", "Only the group owner can add members to the group.");
    public static Error OnlyOwnerCanEditGroup => new Error("OnlyOwnerCanEditGroup", "Only the group owner can edit the group.");
    public static Error OnlyOwnerCanRemoveMembers => new Error("OnlyOwnerCanRemoveMembers", "Only the group owner can remove members from the group.");
    public static Error MemberNotFound => new Error("MemberNotFound", "The specified member was not found in the group.");
    public static Error OnlyOwnerCanDeactivateGroup => new Error("OnlyOwnerCanDeactivateGroup", "Only the group owner can deactivate the group.");
    public static Error GroupAlreadyExists => new Error("GroupAlreadyExists", "A group with the specified name already exists.");
    public static Error UnactiveGroup => new Error("UnactiveGroup", "The group is already inactive.");
}
