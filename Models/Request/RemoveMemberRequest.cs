namespace GroupsMicroservice.Models.Request;

public record RemoveMemberRequest
(
    Guid GroupId,
    Guid UserId
);
