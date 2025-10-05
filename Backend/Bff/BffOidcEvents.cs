using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Backend.Bff;

public sealed class BffOidcEvents : OpenIdConnectEvents
{
    private readonly IBffTokenStore _store;
    private readonly ILogger<BffOidcEvents> _logger;

    public BffOidcEvents(IBffTokenStore store, ILogger<BffOidcEvents> logger)
    {
        _store = store;
        _logger = logger;
    }

    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        if (context.Properties?.Items.TryGetValue("prompt", out var prompt) == true && !string.IsNullOrEmpty(prompt))
            context.ProtocolMessage.Prompt = prompt;

        _logger.LogInformation("OIDC redirect: client_id={client}, redirect_uri={redirect}, auth_endpoint={endpoint}",
            context.ProtocolMessage.ClientId,
            context.ProtocolMessage.RedirectUri,
            context.ProtocolMessage.IssuerAddress);

        return base.RedirectToIdentityProvider(context);
    }

    public override async Task TokenValidated(TokenValidatedContext ctx)
    {
        var sid = Guid.NewGuid().ToString("N");

        // Build a minimal identity to keep the auth cookie small
        var src = ctx.Principal!;
        var sub = src.FindFirst("sub")?.Value
                 ?? src.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? Guid.NewGuid().ToString("N");

        var name = src.FindFirst("name")?.Value
                ?? src.Identity?.Name
                ?? sub;

        var identity = new ClaimsIdentity("oidc", nameType: "name", roleType: "role");
        identity.AddClaim(new Claim("sub", sub));
        identity.AddClaim(new Claim("name", name));
        identity.AddClaim(new Claim("bff.sid", sid));

        ctx.Principal = new ClaimsPrincipal(identity);

        // Store tokens in the server-side store (BFF)
        int expiresIn = int.TryParse(ctx.TokenEndpointResponse?.ExpiresIn, out var parsed)
            ? parsed
            : 300;

        var accessToken = ctx.TokenEndpointResponse?.AccessToken;
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("OIDC: Access token missing in TokenEndpointResponse.");
            return;
        }

        await _store.SaveAsync(sid, new TokenSet
        {
            AccessToken = accessToken!,
            RefreshToken = ctx.TokenEndpointResponse?.RefreshToken,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
        });

        // Keep only id_token for sign-out id_token_hint
        var idToken = ctx.ProtocolMessage.IdToken;
        if (!string.IsNullOrEmpty(idToken))
        {
            ctx.Properties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "id_token", Value = idToken }
            });
        }
    }

    public override async Task RedirectToIdentityProviderForSignOut(RedirectContext context)
    {
        var sid = context.HttpContext.User.FindFirst("bff.sid")?.Value;
        if (!string.IsNullOrEmpty(sid))
        {
            await _store.DeleteAsync(sid);
        }

        await base.RedirectToIdentityProviderForSignOut(context);
    }
}