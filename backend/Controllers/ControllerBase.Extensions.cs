using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
        => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing"));

    public static string GetUsername(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? "unknown";

    public static string GetRole(this ClaimsPrincipal user)
        => user.FindFirstValue("role")
            ?? user.FindFirstValue(ClaimTypes.Role)
            ?? "applicant";
}
