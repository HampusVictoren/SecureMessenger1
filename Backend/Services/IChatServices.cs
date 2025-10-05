using SecureMessenger.Backend.Models;

namespace SecureMessenger.Backend.Services;

public interface IChatService
{
    Task<ChatDto> CreateChatByUsernameAsync(string currentUserId, string currentUsername, string targetUsername);
    Task<IReadOnlyList<ChatDto>> ListChatsAsync(string userId);
    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(string userId, string chatId);
    Task<MessageDto> AddMessageAsync(string userId, string chatId, string content);
}