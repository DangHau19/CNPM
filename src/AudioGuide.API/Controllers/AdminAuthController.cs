using AudioGuide.API.Security;
using AudioGuide.BLL;
using AudioGuide.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuide.API.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly JwtTokenService _jwt;

    public AdminAuthController(IAuthService auth, JwtTokenService jwt)
    {
        _auth = auth;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _auth.LoginAsync(request.Username, request.Password);
        if (user == null) return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." });
        return Ok(new { token = _jwt.Create(user), displayName = user.DisplayName });
    }
}
