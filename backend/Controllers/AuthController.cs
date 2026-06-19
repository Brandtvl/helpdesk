using Helpdesk.Api.DTOs;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Controllers;

// ОБ-1: аутентификация - регистрация и вход
// ОБ-4: серверная валидация входных данных (атрибуты на DTO + ручные проверки)
// ОБ-5: корректные коды ответа (201, 400, 401, 409)
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    // POST /api/auth/register
    // ОБ-1: регистрация нового пользователя
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        // ОБ-4: атрибуты [Required] на DTO уже проверяют поля,
        // но дополнительно проверяем пустые строки
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Все поля обязательны" });

        try
        {
            var result = await authService.RegisterAsync(req);
            return StatusCode(201, result); // 201 Created
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message }); // 409 если email/username уже занят
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // 400 если роль неверная
        }
    }

    // POST /api/auth/login
    // ОБ-1: вход в систему, возвращает JWT токен
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email и пароль обязательны" });

        try
        {
            var result = await authService.LoginAsync(req);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message }); // 401 если пароль неверный
        }
    }
}
