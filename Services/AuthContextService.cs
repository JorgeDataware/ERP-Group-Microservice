using GroupsMicroservice.Services.IServices;
using GroupsMicroservice.Utilities.Constants;
using System.Security.Claims;

namespace GroupsMicroservice.Services;

public class AuthContextService(IHttpContextAccessor httpContextAccessor) : IAuthContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return null;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        return _httpContextAccessor.HttpContext?.User.Claims
            .Any(c => c.Type == CustomClaimTypes.Permission && c.Value == permission) ?? false;
    }

    public IEnumerable<string> GetPermissions()
    {
        return _httpContextAccessor.HttpContext?.User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .Distinct()
            .ToList()
            ?? Enumerable.Empty<string>();
    }
}