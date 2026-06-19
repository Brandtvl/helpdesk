using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Enums;
using Helpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Api.Services;

// ОБ-1: аутентификация с сохранением сессии (JWT токен)
// ОБ-2: ролевое разграничение доступа (applicant, executor, admin)
// ОБ-8: пароли хранятся в виде хеша BCrypt, не в открытом виде
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ОБ-1: регистрация нового пользователя
    // ОБ-8: сразу хешируем пароль, в базу никогда не пишем открытый текст
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        // проверяем уникальность - два пользователя с одним email быть не должно
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            throw new InvalidOperationException("Пользователь с таким email уже существует");

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            throw new InvalidOperationException("Пользователь с таким именем уже существует");

        // ОБ-2: парсим роль из строки
        Role role;
        var roleLower = req.Role.ToLowerInvariant();
        if (roleLower == "applicant")
            role = Role.Applicant;
        else if (roleLower == "executor")
            role = Role.Executor;
        else if (roleLower == "admin")
            role = Role.Admin;
        else
            throw new ArgumentException("Недопустимая роль");

        var user = new User
        {
            Username     = req.Username,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password), // ОБ-8
            Role         = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        return new AuthResponse(user.Id, user.Username, SerializeRole(user.Role), token);
    }

    // ОБ-1: вход - проверяем email+пароль и выдаём токен
    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Неверный email или пароль");

        // ОБ-8: BCrypt.Verify сравнивает введённый пароль с хешем из базы
        bool passwordOk = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!passwordOk)
            throw new UnauthorizedAccessException("Неверный email или пароль");

        var token = GenerateToken(user);
        var userDto = new UserDto(user.Id, user.Username, user.Email, SerializeRole(user.Role), user.CreatedAt.ToString("O"));
        return new LoginResponse(token, userDto);
    }

    // генерируем JWT токен - в него кладём id пользователя, имя и роль
    // токен живёт 24 часа (настраивается в appsettings.json)
    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        int expiresHours = int.Parse(_config["Jwt:ExpiresHours"] ?? "24");
        var expires = DateTime.UtcNow.AddHours(expiresHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, SerializeRole(user.Role)),
            new Claim("role", SerializeRole(user.Role)) // дублируем для фронтенда
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ОБ-2: конвертируем enum роли в строку для JSON и токена
    public static string SerializeRole(Role role)
    {
        if (role == Role.Applicant) return "applicant";
        if (role == Role.Executor)  return "executor";
        if (role == Role.Admin)     return "admin";
        return role.ToString().ToLowerInvariant();
    }
}
