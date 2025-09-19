using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var user = new IdentityUser { UserName = model.Username };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
            return Ok();
        return BadRequest(result.Errors);
    }
}

public class RegisterModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}