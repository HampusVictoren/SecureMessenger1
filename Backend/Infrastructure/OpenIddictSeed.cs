using Microsoft.AspNetCore.Identity;
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var existing = await manager.FindByClientIdAsync("spa", ct);

        // Minimal PKCE + Refresh permissions (no password grant)
        var permissions = new HashSet<string>
        {
            // Endpoints
            "ept:authorization",
            "ept:token",
            "ept:userinfo",

            // Grant types
            "gt:authorization_code",
            "gt:refresh_token",

            // Response types
            "rst:code",

            // Scopes
            "scp:openid",
            "scp:profile",
            "scp:email",
            "scp:offline_access"
        };

        var requirements = new HashSet<string>
        {
            Requirements.Features.ProofKeyForCodeExchange
        };

        if (existing is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "spa",
                DisplayName = "SPA Client",
                ClientType = ClientTypes.Public
            };

            descriptor.RedirectUris.UnionWith(new[]
            {
                new Uri("http://localhost:5173/callback"),
                new Uri("https://oauth.pstmn.io/v1/callback")
            });

            descriptor.PostLogoutRedirectUris.UnionWith(new[]
            {
                new Uri("http://localhost:5173/")
            });

            descriptor.Permissions.UnionWith(permissions);
            descriptor.Requirements.UnionWith(requirements);

            await manager.CreateAsync(descriptor, ct);
        }
        else
        {
            // Load current descriptor from the existing application
            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, existing, ct);

            // Simple fields
            descriptor.DisplayName = "SPA Client";
            descriptor.ClientType = ClientTypes.Public;

            // Collections: clear + add (don't reassign)
            descriptor.RedirectUris.Clear();
            descriptor.RedirectUris.UnionWith(new[]
            {
                new Uri("http://localhost:5173/callback"),
                new Uri("https://oauth.pstmn.io/v1/callback")
            });

            descriptor.PostLogoutRedirectUris.Clear();
            descriptor.PostLogoutRedirectUris.UnionWith(new[]
            {
                new Uri("http://localhost:5173/")
            });

            descriptor.Permissions.Clear();
            descriptor.Permissions.UnionWith(permissions);

            descriptor.Requirements.Clear();
            descriptor.Requirements.UnionWith(requirements);

            await manager.UpdateAsync(existing, descriptor, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
