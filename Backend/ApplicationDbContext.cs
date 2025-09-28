using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Registers OpenIddict default entity sets (no DbSet<> needed unless you query them manually)
        builder.UseOpenIddict();
    }

    // (Optional) If you want to query them directly you can add:
    // public DbSet<OpenIddictEntityFrameworkCoreApplication> OpenIddictApplications { get; set; }
    // public DbSet<OpenIddictEntityFrameworkCoreAuthorization> OpenIddictAuthorizations { get; set; }
    // public DbSet<OpenIddictEntityFrameworkCoreScope> OpenIddictScopes { get; set; }
    // public DbSet<OpenIddictEntityFrameworkCoreToken> OpenIddictTokens { get; set; }
}