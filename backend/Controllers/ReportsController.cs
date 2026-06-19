using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Enums;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-20: отчёты - распределение по статусам, среднее время решения, нагрузка на исполнителей
// ОБ-2: отчёты доступны только администратору
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "admin")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    // GET /api/reports/by-status?from=date&to=date
    // FR-20: распределение обращений по статусам за период
    [HttpGet("by-status")]
    public async Task<IActionResult> ByStatus([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = db.Tickets.AsQueryable();

        // фильтруем по периоду если указан
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(t => t.CreatedAt <= to.Value);

        var counts = await query
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(counts.Select(c => new StatusCountDto(
            TicketStateMachine.Serialize(c.Status), c.Count)));
    }

    // GET /api/reports/avg-resolution?from=date&to=date
    // FR-20: среднее время решения - смотрим когда статус стал "resolved"
    // и считаем разницу с датой создания
    [HttpGet("avg-resolution")]
    public async Task<IActionResult> AvgResolution([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = db.HistoryEntries
            .Where(h => h.Field == "status" && h.NewValue == "resolved");

        if (from.HasValue) query = query.Where(h => h.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(h => h.CreatedAt <= to.Value);

        var resolved = await query
            .Include(h => h.Ticket)
            .ToListAsync();

        if (!resolved.Any()) return Ok(new AvgResolutionDto(0));

        // считаем среднее в часах
        var avgHours = resolved
            .Select(h => (h.CreatedAt - h.Ticket.CreatedAt).TotalHours)
            .Average();

        return Ok(new AvgResolutionDto(Math.Round(avgHours, 1)));
    }

    // GET /api/reports/executor-load?from=date&to=date
    // FR-20: нагрузка на исполнителей - сколько тикетов назначено каждому
    [HttpGet("executor-load")]
    public async Task<IActionResult> ExecutorLoad([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = db.Tickets
            .Where(t => t.AssigneeId != null) // берём только назначенные
            .Include(t => t.Assignee)
            .AsQueryable();

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(t => t.CreatedAt <= to.Value);

        var load = await query
            .GroupBy(t => new { t.AssigneeId, t.Assignee!.Username })
            .Select(g => new ExecutorLoadDto(g.Key.AssigneeId!.Value, g.Key.Username, g.Count()))
            .OrderByDescending(l => l.Count)
            .ToListAsync();

        return Ok(load);
    }
}
