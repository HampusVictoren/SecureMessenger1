using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SecureMessenger.Backend.Models;
using SecureMessenger.Backend.Services;
using System.Security.Claims;

namespace SecureMessenger.Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _service;

    public ChatHub(IChatService service)
    {
        _service = service;
    }

    private (string userId, string username) Current()
    {
        var userId = Context.User?.FindFirstValue("sub") ?? Context.UserIdentifier
                     ?? throw new HubException("Missing user id.");
        var username = Context.User?.FindFirstValue("preferred_username")
                       ?? Context.User?.Identity?.Name
                       ?? "user";
        return (userId, username);
    }

    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
    }

    public async Task SendMessage(string chatId, string content)
    {
        var (userId, _) = Current();
        var msg = await _service.AddMessageAsync(userId, chatId, content);
        await Clients.Group(chatId).SendAsync("ReceiveMessage", msg);
    }
}