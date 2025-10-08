using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SecureMessenger.Backend.Hubs;
using SecureMessenger.Backend.Models;
using SecureMessenger.Backend.Services;
using System.Security.Claims;

namespace SecureMessenger.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly IChatService _service;
    private readonly IHubContext<ChatHub> _hub;

    public ChatsController(IChatService service, IHubContext<ChatHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    //private (string userId, string username) Current()
    //{
    //    var userId = User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();
    //    var username = User.FindFirstValue("preferred_username") ?? User.Identity?.Name ?? "user";
    //    return (userId, username);
    //}
    //private (string userId, string username) TryCurrent()
    //{
    //    var userId = User.FindFirstValue("sub")
    //                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
    //                 ?? throw new UnauthorizedAccessException();

    //    var username = User.FindFirstValue("preferred_username")
    //                  ?? User.Identity?.Name
    //                  ?? "user";
    //    return (userId, username);
    //}

    private (bool ok, string? userId, string username) TryCurrent()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var username = User.FindFirstValue("preferred_username") ?? User.Identity?.Name ?? "user";
        return (userId is not null, userId, username);
    }


    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChatDto>>> List()
    {
        var (ok, userId, _) = TryCurrent();
        if (!ok) return Unauthorized();
        var chats = await _service.ListChatsAsync(userId!);
        return Ok(chats);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest("Username is required.");

        var (ok, userId, username) = TryCurrent();
        if (!ok) return Unauthorized();

        var chat = await _service.CreateChatByUsernameAsync(userId!, username, req.Username.Trim());
        return Ok(new { chat });
    }

    [HttpGet("{id}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> Messages(string id)
    {
        var (ok, userId, _) = TryCurrent();
        if (!ok) return Unauthorized();

        var messages = await _service.GetMessagesAsync(userId!, id);
        return Ok(messages);
    }

    [HttpPost("{id}/messages")]
    public async Task<ActionResult> Send(string id, [FromBody] SendMessageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest("Content is required.");

        var (ok, userId, _) = TryCurrent();
        if (!ok) return Unauthorized();

        var msg = await _service.AddMessageAsync(userId!, id, req.Content.Trim());
        await _hub.Clients.Group(id).SendAsync("ReceiveMessage", msg);
        return Ok(new { message = msg });
    }
}