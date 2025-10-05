using Backend.Bff;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SecureMessenger.Backend.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.AddFilter("OpenIddict", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Identity/Account/Login";
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.HttpOnly = true;
});

// OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(o =>
    {
        o.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    })
    .AddServer(o =>
    {
        o.SetAuthorizationEndpointUris("connect/authorize")
         .SetTokenEndpointUris("connect/token")
         .SetUserInfoEndpointUris("connect/userinfo")
         .SetEndSessionEndpointUris("connect/logout");
         //.SetRevocationEndpointUris("connect/revocation");

        o.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        o.AllowRefreshTokenFlow();

        o.SetAccessTokenLifetime(TimeSpan.FromMinutes(5));
        o.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        o.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess
        );

        o.AddDevelopmentEncryptionCertificate()
         .AddDevelopmentSigningCertificate();

        o.UseAspNetCore()
         .EnableAuthorizationEndpointPassthrough()
         .EnableTokenEndpointPassthrough()
         .EnableUserInfoEndpointPassthrough()
         .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(o =>
    {
        o.UseLocalServer();
        o.UseAspNetCore();
    });

builder.Services.AddControllers();
builder.Services.AddSignalR();
//builder.Services.AddControllers(options =>
//{
//    // Require auth globally; adjust if needed
//    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
//    options.Filters.Add(new AuthorizeFilter(policy));
//});

builder.Services.AddSingleton<IChatService, InMemoryChatService>();

// CORS (not needed for same-origin, but harmless if kept)
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// OpenAPI, Identity UI
builder.Services.AddOpenApi();
builder.Services.AddHostedService<Backend.Infrastructure.OpenIddictSeed>();
builder.Services.AddRazorPages();

// BFF infra
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddTransient<BffOidcEvents>();
builder.Services.AddSingleton<IBffTokenStore, MemoryBffTokenStore>();
builder.Services.AddSingleton<IBffTokenRefresher, BffTokenRefresher>();

// Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "BffCookie";
    options.DefaultChallengeScheme = "bff-oidc";
})
.AddCookie("BffCookie", options =>
{
    options.LoginPath = "/bff/login";                 // <-- send browser to BFF login (starts OIDC)
    options.AccessDeniedPath = "/";                   // optional
    options.Cookie.Name = "BffCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;           // tighter when same-origin
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);   // align with your UX
    options.SlidingExpiration = true;                // keep active sessions alive
    options.Events.OnRedirectToLogin = ctx =>
    {
        var accept = ctx.Request.Headers["Accept"].ToString();
        var isHtml = accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);

        var isAjax = string.Equals(
            ctx.Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);

        var isSignalR = ctx.Request.Path.StartsWithSegments("/hubs")
                        || ctx.Request.Query.ContainsKey("negotiate")
                        || string.Equals(ctx.Request.Headers["Upgrade"], "websocket", StringComparison.OrdinalIgnoreCase);

        var wantsJson = accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);

        // For any non-HTML/programmatic calls, return 401 instead of redirecting
        if (!isHtml || isAjax || isSignalR || wantsJson)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect("bff-oidc", options =>
{
    options.Authority = builder.Configuration["Auth:Authority"] ?? "https://localhost:7235";
    options.ClientId = "spa";
    options.ResponseType = "code";
    options.ResponseMode = "query";
    options.UsePkce = true;
    options.CallbackPath = "/bff/callback";

    options.MapInboundClaims = false;
    options.SaveTokens = false;
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    options.GetClaimsFromUserInfoEndpoint = false;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    //options.Scope.Add("email");
    options.Scope.Add("offline_access");
    options.SignInScheme = "BffCookie";

    options.EventsType = typeof(BffOidcEvents);

    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "role";

    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

    options.NonceCookie.SameSite = SameSiteMode.None;
    options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

    // error handling if the user cancels or OP returns an error
    options.Events ??= new OpenIdConnectEvents();
    options.Events.OnRemoteFailure = ctx =>
    {
        ctx.HandleResponse();
        ctx.Response.Redirect("/?loginError=1");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("bff", policy =>
    {
        policy.AddAuthenticationSchemes("BffCookie");
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));

// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF";
    options.Cookie.Name = "__Host-Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Lax;           // OK for same-origin SPA
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
});

// SPA static files for production
builder.Services.AddSpaStaticFiles(options =>
{
    // Point to the SPA build output folder (see Svelte config below)
    options.RootPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Frontend", "build");
});

var app = builder.Build();

// Dev OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseSpaStaticFiles(); // serve SPA static assets in production

app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<SecureMessenger.Backend.Hubs.ChatHub>("/hubs/chat");

app.MapControllers();
app.MapRazorPages();

// BFF endpoints
app.MapBffEndpoints();

/*
 // CSRF validation on unsafe BFF methods — disabled.
 // Reason: there are no unsafe methods under /bff anymore (only GET endpoints like /bff/login, /bff/user, /bff/userinfo, /bff/signout).
 // Razor Pages/Identity antiforgery still applies where needed via their own filters/middleware.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/bff") &&
        (HttpMethods.IsPost(ctx.Request.Method) ||
         HttpMethods.IsPut(ctx.Request.Method) ||
         HttpMethods.IsPatch(ctx.Request.Method) ||
         HttpMethods.IsDelete(ctx.Request.Method)))
    {
        await ctx.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>()
                                 .ValidateRequestAsync(ctx);
    }
    await next();
});
*/

// signout
app.MapGet("/signout", (HttpContext http) =>
{
    var props = new AuthenticationProperties { RedirectUri = "/" };
    return Results.SignOut(props, new[] { "BffCookie", "bff-oidc" });
}).AllowAnonymous();

// SPA fallback and dev proxy (only for non-API/identity paths)
app.MapWhen(ctx =>
    !ctx.Request.Path.StartsWithSegments("/bff") &&
    !ctx.Request.Path.StartsWithSegments("/connect") &&
    !ctx.Request.Path.StartsWithSegments("/Identity"), //&&
    //!ctx.Request.Path.StartsWithSegments("/__routes"),
    spaApp =>
    {
        spaApp.UseSpa(spa =>
        {
            spa.Options.SourcePath = Path.Combine(builder.Environment.ContentRootPath, "..", "Frontend");
            if (app.Environment.IsDevelopment())
            {
                // Match the protocol Vite prints in its console ("Local: https://localhost:5173" or http)
                spa.UseProxyToSpaDevelopmentServer("https://localhost:5173");
            }
        });
    });





if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Run();





















































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































