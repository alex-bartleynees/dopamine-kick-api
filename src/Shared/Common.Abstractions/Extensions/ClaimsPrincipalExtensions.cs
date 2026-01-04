using System.Security.Claims;

namespace Common.Abstractions.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(subClaim, out var userId) ? userId : null;
    }
}
