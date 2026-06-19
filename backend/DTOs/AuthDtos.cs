using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Api.DTOs;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)] string Username,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(6), MaxLength(100)] string Password,
    [Required] string Role
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(int Id, string Username, string Role, string Token);

public record LoginResponse(string Token, UserDto User);

public record UserDto(int Id, string Username, string Email, string Role, string CreatedAt);
