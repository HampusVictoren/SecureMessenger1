using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Backend.Bff;

public static class BffEndpoints
{
    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        var bff = app.MapGroup("/bff");


        //bff.MapGet("/login", async (HttpContext ctx) =>
        //{
        //    var returnUrl = ctx.Request.Query["returnUrl"].FirstOrDefault();
        //    var redirect = IsLocalUrl(returnUrl) ? returnUrl! : "/"; // stay on 7235
        //    await ctx.ChallengeAsync("bff-oidc", new AuthenticationProperties { RedirectUri = redirect });
        //}).AllowAnonymous();

        bff.MapGet("/login", async (HttpContext ctx) =>
        {
            var returnUrl = ctx.Request.Query["returnUrl"].FirstOrDefault();
            var redirect = IsLocalUrl(returnUrl) ? returnUrl! : "/";

            var props = new AuthenticationProperties { RedirectUri = redirect };

            // If caller asked for a fresh sign-in, set prompt=login for OP
            if (string.Equals(ctx.Request.Query["reauth"], "true", StringComparison.OrdinalIgnoreCase))
                props.Items["prompt"] = "login";

            await ctx.ChallengeAsync("bff-oidc", props);
        }).AllowAnonymous();

        bff.MapGet("/user", (ClaimsPrincipal user) =>
        {
            return Results.Json(new
            {
                name = user.Identity?.Name,
                claims = user.Claims
                             .Where(c => c.Type != "bff.sid") // don't leak BFF store key
                             .Select(c => new { c.Type, c.Value })
            });
        }).RequireAuthorization("bff");

        bff.MapGet("/userinfo", async (
            HttpContext http,
            IBffTokenStore store,
            IBffTokenRefresher refresher,
            IHttpClientFactory factory,
            IOptionsMonitor<OpenIdConnectOptions> oidcOptions) =>
        {
            var sid = http.User.FindFirst("bff.sid")?.Value;
            if (string.IsNullOrEmpty(sid)) return Results.Unauthorized();

            var tokens = await store.GetAsync(sid);
            if (tokens is null) return Results.Unauthorized();

            // Ensure tokens are valid (refresh if needed)
            var (ok, current) = await refresher.EnsureValidTokensAsync(sid, tokens, http.RequestAborted);
            if (!ok) return Results.Unauthorized();

            var authority = oidcOptions.Get("bff-oidc").Authority?.TrimEnd('/');
            if (string.IsNullOrEmpty(authority)) return Results.Problem("OIDC authority not configured.", statusCode: 500);

            var client = factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, $"{authority}/connect/userinfo");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);
            var resp = await client.SendAsync(req, http.RequestAborted);

            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";

            http.Response.StatusCode = (int)resp.StatusCode;
            http.Response.ContentType = contentType;

            await using var responseStream = await resp.Content.ReadAsStreamAsync(http.RequestAborted);
            await responseStream.CopyToAsync(http.Response.Body, http.RequestAborted);

            return Results.Empty;
        }).RequireAuthorization("bff");


        // RP-initiated logout (top-level navigation)
        bff.MapGet("/signout", (HttpContext http) =>
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = "/" // must be registered as post-logout redirect (you have /signout-callback-oidc too)
            };
            return Results.SignOut(props, new[] { "BffCookie", "bff-oidc" });
        }).AllowAnonymous();


        return app;
    }

    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) return true;
        if (url.Length > 1 && url[0] == '~' && url[1] == '/') return true;
        return false;
    }

}