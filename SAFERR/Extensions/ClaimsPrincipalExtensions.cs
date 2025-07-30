using System;
using System.Security.Claims;

namespace SAFERR.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : (Guid?)null;
    }

    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static Guid? GetUserBrandId(this ClaimsPrincipal user)
    {
        var brandIdClaim = user.FindFirst("BrandId")?.Value;
        return Guid.TryParse(brandIdClaim, out var brandId) ? brandId : (Guid?)null;
    }
}
