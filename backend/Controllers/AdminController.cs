using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-21: управление справочниками и нормативами (категории, SLA) — только admin
// FR-22: принудительная смена статуса администратором без проверки таблицы переходов
// ОБ-2: весь контроллер защищён [Authorize(Roles = "admin")]
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(AppDbContext db, TicketService ticketService, AuditService audit) : ControllerBase
{
    // GET /api/admin/users
    // FR-21: администратор видит список всех пользователей системы
    // ОБ-2: обычные пользователи этот эндпоинт не видят
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await db.Users
            .OrderBy(u => u.Username)
            .ToListAsync();

        return Ok(users.Select(u => new UserDto(
            u.Id, u.Username, u.Email,
            AuthService.SerializeRole(u.Role),
            u.CreatedAt.ToString("O"))));
    }

    // PATCH /api/admin/tickets/{id}/force
    // FR-22: принудительная смена статуса/исполнителя — обходит таблицу переходов
    // это нужно когда тикет завис в неправильном статусе или нужно экстренно его закрыть
    [HttpPatch("tickets/{id:int}/force")]
    public async Task<IActionResult> Force(int id, [FromBody] AdminForceRequest req)
    {
        // причина обязательна - для журнала аудита (ОБ-7)
        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { error = "Причина обязательна" });

        var userId   = User.GetUserId();
        var username = User.GetUsername();

        try
        {
            TicketDto result = null!;

            if (!string.IsNullOrEmpty(req.Status))
            {
                // FR-22: isAdmin=true — смена без проверки допустимости перехода
                var statusReq = new ChangeStatusRequest(req.Status, $"[Принудительно] {req.Reason}");
                result = await ticketService.ChangeStatusAsync(id, statusReq, userId, username, isAdmin: true);
            }

            if (req.AssigneeId.HasValue || req.Status is null)
            {
                result = await ticketService.AssignAsync(id, req.AssigneeId, userId, username);
            }

            // ОБ-7: журналируем принудительное действие администратора
            audit.Log(userId, username, "force_change", "ticket", id.ToString());
            await db.SaveChangesAsync();

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
