namespace SecureMessenger.Backend.Models;

public class Chat
{
    public string Id { get; set; } = default!;
    public string? Title { get; set; }
    public string? PairKey { get; set; }  // Unique for 1:1 chats (sorted "userA|userB")
    public DateTimeOffset UpdatedAt { get; set; }

    public string? LastMessageId { get; set; }
    public Message? LastMessage { get; set; }

    public List<ChatParticipant> Participants { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class ChatParticipant
{
    public string ChatId { get; set; } = default!;
    public Chat Chat { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class Message
{
    public string Id { get; set; } = default!;
    public string ChatId { get; set; } = default!;
    public Chat Chat { get; set; } = default!;
    public string SenderId { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTimeOffset SentAt { get; set; }
    public string? Status { get; set; }
}