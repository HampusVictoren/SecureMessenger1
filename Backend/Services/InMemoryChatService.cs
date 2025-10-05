using System.Collections.Concurrent;
using SecureMessenger.Backend.Models;

namespace SecureMessenger.Backend.Services;

public class InMemoryChatService : IChatService
{
    private readonly ConcurrentDictionary<string, UserDto> _users = new();
    private readonly ConcurrentDictionary<string, ChatDto> _chats = new();
    private readonly ConcurrentDictionary<string, List<MessageDto>> _messages = new();

    private static string NewId() => Guid.NewGuid().ToString("N");

    public Task<ChatDto> CreateChatByUsernameAsync(string currentUserId, string currentUsername, string targetUsername)
    {
        var me = _users.GetOrAdd(currentUserId, _ => new UserDto(currentUserId, currentUsername, currentUsername, null));
        var otherId = $"user-{targetUsername.ToLowerInvariant()}";
        var other = _users.GetOrAdd(otherId, _ => new UserDto(otherId, targetUsername, targetUsername, null));

        // Try to find existing chat with exactly these two participants
        var existing = _chats.Values.FirstOrDefault(c =>
        {
            var ids = c.Participants.Select(p => p.Id).OrderBy(x => x).ToArray();
            var target = new[] { me.Id, other.Id }.OrderBy(x => x).ToArray();
            return ids.SequenceEqual(target);
        });

        if (existing is not null)
            return Task.FromResult(existing);

        var chatId = $"chat-{NewId()}";
        var participants = new List<UserDto> { me, other };
        var chat = new ChatDto(
            Id: chatId,
            Title: null,
            Participants: participants,
            LastMessage: null,
            UnreadCount: 0,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        _chats[chatId] = chat;
        _messages[chatId] = new List<MessageDto>();
        return Task.FromResult(chat);
    }

    public Task<IReadOnlyList<ChatDto>> ListChatsAsync(string userId)
    {
        var list = _chats.Values
            .Where(c => c.Participants.Any(p => p.Id == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ChatDto>>(list);
    }

    public Task<IReadOnlyList<MessageDto>> GetMessagesAsync(string userId, string chatId)
    {
        if (!_chats.TryGetValue(chatId, out var chat) || !chat.Participants.Any(p => p.Id == userId))
            return Task.FromResult<IReadOnlyList<MessageDto>>(Array.Empty<MessageDto>());

        var msgs = _messages.GetValueOrDefault(chatId) ?? new List<MessageDto>();
        var ordered = msgs.OrderBy(m => m.SentAt).ToList();
        return Task.FromResult<IReadOnlyList<MessageDto>>(ordered);
    }

    public Task<MessageDto> AddMessageAsync(string userId, string chatId, string content)
    {
        if (!_chats.TryGetValue(chatId, out var chat) || !chat.Participants.Any(p => p.Id == userId))
            throw new InvalidOperationException("Not a participant of this chat.");

        var msg = new MessageDto(
            Id: $"msg-{NewId()}",
            ChatId: chatId,
            SenderId: userId,
            Content: content,
            SentAt: DateTimeOffset.UtcNow,
            Status: "sent"
        );

        var list = _messages.GetOrAdd(chatId, _ => new List<MessageDto>());
        lock (list)
        {
            list.Add(msg);
        }

        // Update chat metadata
        var updated = chat with { LastMessage = msg, UpdatedAt = msg.SentAt };
        _chats[chatId] = updated;

        return Task.FromResult(msg);
    }
}