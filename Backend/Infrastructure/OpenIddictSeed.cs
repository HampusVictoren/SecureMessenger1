using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Backend.Infrastructure;

public sealed class OpenIddictSeed : IHostedService
{
    private readonly IServiceProvider _sp;
    public OpenIddictSeed(IServiceProvider sp) => _sp = sp;

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var logger  = scope.ServiceProvider.GetRequiredService<ILogger<OpenIddictSeed>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var existing = await manager.FindByClientIdAsync("spa", ct);

        var permissions = new HashSet<string>
        {
            // Endpoints
            "ept:authorization",
            "ept:token",
            "ept:userinfo",
            "ept:logout",
            //"ept:revocation",

            // Grants
            "gt:authorization_code",
            "gt:refresh_token",

            // Response types (add both to be version-agnostic)
            "rsp:code",
            "rst:code",


            // Scopes
            "scp:openid",
            "scp:profile",
            //"scp:email",
            "scp:offline_access"
        };

        var requirements = new HashSet<string> { Requirements.Features.ProofKeyForCodeExchange };

        // Backend callbacks (must match OIDC options.CallbackPath = "/bff/callback")
        var bffRedirects = new[]
        {
            new Uri("https://localhost:7235/bff/callback")
        };

        // Post-logout redirects
        var postLogoutRedirects = new[]
        {
            new Uri("https://localhost:7235/signout-callback-oidc"), // OIDC signout callback
            new Uri("https://localhost:7235/")                       // optional SPA landing
        };

        if (existing is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "spa",
                DisplayName = "SPA Client",
                ClientType = ClientTypes.Public
            };

            descriptor.RedirectUris.UnionWith(bffRedirects);
           // descriptor.RedirectUris.UnionWith(postmanRedirects);
            descriptor.PostLogoutRedirectUris.UnionWith(postLogoutRedirects);
            descriptor.Permissions.UnionWith(permissions);
            descriptor.Requirements.UnionWith(requirements);

            await manager.CreateAsync(descriptor, ct);
            logger.LogInformation("Created OpenIddict app 'spa' with redirect_uris: {uris}", string.Join(", ", descriptor.RedirectUris));

            var created = await manager.FindByClientIdAsync("spa", ct);
            var perms = await manager.GetPermissionsAsync(created!, ct);
            logger.LogInformation("Client 'spa' permissions: {perms}", string.Join(", ", perms));
        }
        else
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, existing, ct);

            descriptor.DisplayName = "SPA Client";
            descriptor.ClientType = ClientTypes.Public;

            descriptor.RedirectUris.Clear();
            descriptor.RedirectUris.UnionWith(bffRedirects);
            //descriptor.RedirectUris.UnionWith(postmanRedirects);

            descriptor.PostLogoutRedirectUris.Clear();
            descriptor.PostLogoutRedirectUris.UnionWith(postLogoutRedirects);

            descriptor.Permissions.Clear();
            descriptor.Permissions.UnionWith(permissions);

            descriptor.Requirements.Clear();
            descriptor.Requirements.UnionWith(requirements);

            await manager.UpdateAsync(existing, descriptor, ct);
            logger.LogInformation("Updated OpenIddict app 'spa' with redirect_uris: {uris}", string.Join(", ", descriptor.RedirectUris));

            var updated = await manager.FindByClientIdAsync("spa", ct);
            var perms = await manager.GetPermissionsAsync(updated!, ct);
            logger.LogInformation("Client 'spa' permissions: {perms}", string.Join(", ", perms));
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
