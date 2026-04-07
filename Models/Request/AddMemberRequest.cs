namespace GroupsMicroservice.Models.Request;

public record AddMemberRequest
(
    Guid GroupId,
    Guid memberId
);
