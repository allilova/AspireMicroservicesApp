using Shop.Web.Date;
using System.Security.Claims;

namespace Shop.Web.Datе;

public static class AuthHelper
{
    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id!),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }

    // позволява само локални адреси, за да няма пренасочване към външен сайт
    public static string SafeReturnUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
            ? url : "/";
}