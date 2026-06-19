using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-12: история изменений обращения — фиксируются все смены статуса, исполнителя и т.д.
// ОБ-7: использует те же записи что и журнал аудита, просто смотрим по ticketId
[ApiController]
[Route("api/tickets/{ticketId:int}/history")]
[Authorize] // ОБ-1: авторизация обязательна
public class HistoryController(AppDbContext db) : ControllerBase
{
    // GET /api/tickets/{ticketId}/history
    // FR-12: вся история изменений по конкретному тикету, отсортировано по времени
    [HttpGet]
    public async Task<IActionResult> GetHistory(int ticketId)
    {
        var entries = await db.HistoryEntries
            .Include(h => h.Author)
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.CreatedAt)  // от старых к новым
            .ToListAsync();

        return Ok(entries.Select(h => new HistoryEntryDto(
            h.Id, h.TicketId, h.AuthorId, h.Author?.Username,
            h.Field, h.OldValue, h.NewValue, h.Comment,
            h.CreatedAt.ToString("O"))));
    }
}
