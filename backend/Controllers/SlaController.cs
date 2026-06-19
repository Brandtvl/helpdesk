using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Enums;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-14: нормативы реакции и решения задаются для каждого приоритета
// FR-21: управление нормативами SLA - только администратор может менять
[ApiController]
[Route("api/sla")]
[Authorize]
public class SlaController(AppDbContext db, AuditService audit) : ControllerBase
{
    // GET /api/sla
    // FR-14: получить текущие нормативы для всех приоритетов
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var configs = await db.SlaConfigs.OrderBy(s => s.Priority).ToListAsync();
        return Ok(configs.Select(s => new SlaDto(
            s.Priority.ToString().ToLowerInvariant(),
            s.ReactionHours,
            s.ResolutionHours)));
    }

    // PUT /api/sla/{priority}
    // FR-14, FR-21: обновить норматив для конкретного приоритета - только admin
    [HttpPut("{priority}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string priority, [FromBody] UpdateSlaRequest req)
    {
        // парсим приоритет из строки в enum
        var parsed = priority.ToLowerInvariant() switch
        {
            "low"      => Priority.Low,
            "medium"   => Priority.Medium,
            "high"     => Priority.High,
            "critical" => Priority.Critical,
            _ => (Priority?)null
        };

        if (parsed is null) return BadRequest(new { error = "Неизвестный приоритет" });

        var config = await db.SlaConfigs.FirstOrDefaultAsync(s => s.Priority == parsed.Value);
        if (config is null) return NotFound(new { error = "Норматив не найден" });

        config.ReactionHours   = req.ReactionHours;
        config.ResolutionHours = req.ResolutionHours;

        // ОБ-7: журналируем изменение норматива
        audit.Log(User.GetUserId(), User.GetUsername(), "update_sla", "sla", priority);
        await db.SaveChangesAsync();

        return Ok(new SlaDto(priority, config.ReactionHours, config.ResolutionHours));
    }
}
