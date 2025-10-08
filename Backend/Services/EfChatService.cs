using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureMessenger.Backend.Models;

namespace SecureMessenger.Backend.Services;

public class EfChatService : IChatService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _users;

    public EfChatService(ApplicationDbContext db, UserManager<IdentityUser> users)
    {
        _db = db;
        _users = users;
    }

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static string MakePairKey(string a, string b)
    {
        if (string.CompareOrdinal(a, b) <= 0) return $"{a}|{b}";
        return $"{b}|{a}";
    }

    private static ChatDto ToDto(Chat c)
        => new(
            c.Id,
            c.Title,
            c.Participants
                .Select(p => new UserDto(p.UserId, p.Username, p.DisplayName, p.AvatarUrl))
                .ToList(),
            c.LastMessage is null
                ? null
                : new MessageDto(c.LastMessage.Id, c.LastMessage.ChatId, c.LastMessage.SenderId, c.LastMessage.Content, c.LastMessage.SentAt, c.LastMessage.Status),
            0,
            c.UpdatedAt
        );

    private static MessageDto ToDto(Message m)
        => new(m.Id, m.ChatId, m.SenderId, m.Content, m.SentAt, m.Status);

    public async Task<ChatDto> CreateChatByUsernameAsync(string currentUserId, string currentUsername, string targetUsername)
    {
        // Resolve target user by username in Identity
        var target = await _users.Users.FirstOrDefaultAsync(u => u.UserName == targetUsername);
        if (target is null)
            throw new InvalidOperationException("Target user not found.");

        // Use PairKey to find existing 1:1 chat
        var pairKey = MakePairKey(currentUserId, target.Id);
        var existing = await _db.Chats
            .Include(c => c.Participants)
            .Include(c => c.LastMessage)
            .FirstOrDefaultAsync(c => c.PairKey == pairKey);

        if (existing is not null)
            return ToDto(existing);

        var me = await _users.FindByIdAsync(currentUserId) ?? throw new InvalidOperationException("Current user not found.");

        var chat = new Chat
        {
            Id = NewId("chat"),
            Title = null,
            PairKey = pairKey, // enforce unique 1:1 chat
            UpdatedAt = DateTimeOffset.UtcNow,
            Participants =
            {
                new ChatParticipant { ChatId = default!, UserId = me.Id, Username = me.UserName ?? currentUsername, DisplayName = me.UserName },
                new ChatParticipant { ChatId = default!, UserId = target.Id, Username = target.UserName ?? targetUsername, DisplayName = target.UserName }
            }
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        return ToDto(chat);
    }

    public async Task<IReadOnlyList<ChatDto>> ListChatsAsync(string userId)
    {
        var list = await _db.Chats
            .Include(c => c.Participants)
            .Include(c => c.LastMessage)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(string userId, string chatId)
    {
        var isParticipant = await _db.ChatParticipants.AnyAsync(p => p.ChatId == chatId && p.UserId == userId);
        if (!isParticipant) return Array.Empty<MessageDto>();

        var msgs = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return msgs.Select(ToDto).ToList();
    }

    public async Task<MessageDto> AddMessageAsync(string userId, string chatId, string content)
    {
        var isParticipant = await _db.ChatParticipants.AnyAsync(p => p.ChatId == chatId && p.UserId == userId);
        if (!isParticipant) throw new InvalidOperationException("Not a participant of this chat.");

        var msg = new Message
        {
            Id = NewId("msg"),
            ChatId = chatId,
            SenderId = userId,
            Content = content,
            SentAt = DateTimeOffset.UtcNow,
            Status = "sent"
        };

        _db.Messages.Add(msg);

        var chat = await _db.Chats.FirstAsync(c => c.Id == chatId);
        chat.LastMessageId = msg.Id;
        chat.UpdatedAt = msg.SentAt;

        await _db.SaveChangesAsync();
        return ToDto(msg);
    }
}