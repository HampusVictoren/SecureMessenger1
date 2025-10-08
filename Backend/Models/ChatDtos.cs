namespace SecureMessenger.Backend.Models;

public record UserDto(string Id, string Username, string? DisplayName, string? AvatarUrl);
public record MessageDto(string Id, string ChatId, string SenderId, string Content, DateTimeOffset SentAt, string? Status = "sent");
public record ChatDto(string Id, string? Title, List<UserDto> Participants, MessageDto? LastMessage, int UnreadCount, DateTimeOffset UpdatedAt);

public record CreateChatRequest(string Username);
public record SendMessageRequest(string Content);