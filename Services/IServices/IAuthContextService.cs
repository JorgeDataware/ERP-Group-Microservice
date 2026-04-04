namespace GroupsMicroservice.Services.IServices;

public interface IAuthContextService
{
    Guid? GetUserId();
    bool HasPermission(string permission);
    IEnumerable<string> GetPermissions();
}