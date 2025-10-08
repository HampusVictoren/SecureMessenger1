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

    private (bool ok, string? userId, string username) TryCurrent()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        var username = Context.User?.FindFirstValue("preferred_username")
                       ?? Context.User?.Identity?.Name
                       ?? "user";
        return (userId is not null, userId, username);
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
        var (ok, userId, _) = TryCurrent();
        if (!ok) throw new HubException("Unauthorized");

        var msg = await _service.AddMessageAsync(userId!, chatId, content);
        await Clients.Group(chatId).SendAsync("ReceiveMessage", msg);
    }
}