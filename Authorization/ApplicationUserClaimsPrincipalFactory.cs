using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IMS.Authorization;

/// <summary>
/// Adds app-specific claims so views using <c>User.FindFirst("IsAdmin")</c> match <see cref="ApplicationUser"/> in the database.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("IsAdmin", user.IsAdmin.ToString()));
        if (user.LegacyUserId.HasValue)
            identity.AddClaim(new Claim("UserId", user.LegacyUserId.Value.ToString()));
        return identity;
    }
}
