using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureMessenger.Backend.Models;

public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict();

        builder.Entity<Chat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(256);

            e.Property(x => x.PairKey).HasMaxLength(200);
            e.HasIndex(x => x.PairKey)
                .IsUnique()
                .HasFilter("\"PairKey\" IS NOT NULL"); // quote the identifier

            e.HasMany(x => x.Participants).WithOne(x => x.Chat).HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Messages).WithOne(x => x.Chat).HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LastMessage).WithMany().HasForeignKey(x => x.LastMessageId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.UpdatedAt);
        });

        builder.Entity<ChatParticipant>(e =>
        {
            e.HasKey(x => new { x.ChatId, x.UserId });
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.Username).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(256);
        });

        builder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.SenderId).HasMaxLength(64);
            e.Property(x => x.Content).HasMaxLength(4000);
        });
    }
}