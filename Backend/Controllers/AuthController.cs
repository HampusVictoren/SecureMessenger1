using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
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

    [HttpPost("token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null) return BadRequest();

        if (request.IsPasswordGrantType())
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            principal.SetScopes(Scopes.OpenId, Scopes.OfflineAccess);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest();
    }
}