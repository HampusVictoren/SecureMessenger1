using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

[ApiController]
[Route("connect")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // Authorization endpoint (GET). Called by browser redirect.
    [HttpGet("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null) return BadRequest();

        // If user not signed in -> challenge (this triggers your cookie/Identity flow).
        if (User.Identity?.IsAuthenticated != true)
            return Challenge(
            authenticationSchemes: new[] { IdentityConstants.ApplicationScheme },
            properties: new AuthenticationProperties
            {
            RedirectUri = HttpContext.Request.PathBase + HttpContext.Request.Path +
                      QueryString.Create(Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()))
             });

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var principal = await CreatePrincipalAsync(user, request!.GetScopes());

        // Automatically accept (no consent screen). Implement consent UI if desired.
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // Token endpoint (Authorization Code / Refresh Token / (optional) Password).
    [HttpPost("token"), Produces("application/json")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null) return BadRequest();

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // OpenIddict already validated the code/refresh token.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal;
            if (principal is null)
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // You can rotate/update claims here if needed (e.g., updated roles).
            if (request.IsRefreshTokenGrantType())
            {
                // Re-fetch user to ensure still valid.
                var userId = principal.GetClaim(Claims.Subject);
                if (userId is not null)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user is null || !await _userManager.IsEmailConfirmedAsync(user)) // Example extra check.
                        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    // Optionally rebuild principal to reflect new claims/roles.
                    principal = await CreatePrincipalAsync(user, principal.GetScopes());
                }
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = Errors.UnsupportedGrantType, error_description = "Unsupported grant type." });
    }

    // UserInfo endpoint (requires valid access token with openid/profile/email scopes as appropriate).
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        var sub = User.FindFirst(Claims.Subject)?.Value;
        if (sub is null)
            return BadRequest();

        var user = await _userManager.FindByIdAsync(sub);
        if (user is null)
            return BadRequest();

        var claims = new Dictionary<string, object?>
        {
            ["sub"] = sub
        };

        if (User.HasScope(Scopes.Email))
            claims["email"] = await _userManager.GetEmailAsync(user);

        if (User.HasScope(Scopes.Profile))
            claims["name"] = user.UserName;

        return Ok(claims);
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(IdentityUser user, IEnumerable<string> requestedScopes)
    {
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Add extra claims (roles etc.)
        var roles = await _userManager.GetRolesAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var role in roles)
            identity.AddClaim(new Claim(Claims.Role, role));

        // Standard subject claim (ensure exists).
        if (!identity.HasClaim(c => c.Type == Claims.Subject))
            identity.AddClaim(new Claim(Claims.Subject, user.Id));

        // Determine granted scopes (intersection).
        var allowable = new[]
        {
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            Scopes.OfflineAccess
        };
        var granted = requestedScopes.Intersect(allowable).ToHashSet();

        principal.SetScopes(granted);

        // Resources (APIs) associated with the token (adjust as needed).
        principal.SetResources("api");

        // Claim destinations (required so claims flow into id_token / access_token appropriately).
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        // Minimal example; expand if needed.
        if (claim.Type is Claims.Name or Claims.Subject)
        {
            yield return Destinations.AccessToken;
            if (principal.HasScope(Scopes.OpenId))
                yield return Destinations.IdentityToken;
        }
        else if (claim.Type == Claims.Role)
        {
            yield return Destinations.AccessToken;
        }
        else if (claim.Type == Claims.Email && principal.HasScope(Scopes.Email))
        {
            yield return Destinations.AccessToken;
            yield return Destinations.IdentityToken;
        }
    }

    [HttpGet("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> LogoutGet()
    {
        // Clear the OP’s local session so the next authorize won’t silently re-login.
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        // Complete the OIDC end-session flow (redirects to post_logout_redirect_uri).
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    //[HttpPost("logout")]
    //[IgnoreAntiforgeryToken]
    //public async Task<IActionResult> LogoutPost()
    //{
    //    await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    //    return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    //}
}

// Small helpers
internal static class ClaimsPrincipalExtensions
{
    public static bool HasScope(this ClaimsPrincipal principal, string scope) =>
        principal.HasClaim(OpenIddictConstants.Claims.Scope, scope) ||
        principal.GetScopes().Contains(scope);
}