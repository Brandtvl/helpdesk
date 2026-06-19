using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Models;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-1:  создание обращения
// FR-6:  набор статусов и переходы
// FR-7:  таблица переходов - недопустимый переход = 409
// FR-8:  повторное открытие (reopen)
// FR-10: назначение исполнителя
// FR-13: массовые операции
// FR-17: список с фильтрами
// FR-18: мои обращения
// FR-19: полнотекстовый поиск
// FR-23: зависимости (блокировки)
// ОБ-1:  все маршруты защищены [Authorize]
// ОБ-2:  разграничение по ролям через [Authorize(Roles = "...")]
// ОБ-5:  правильные HTTP коды ответа
// ОБ-6:  пагинация, фильтрация, сортировка
[ApiController]
[Route("api/tickets")]
[Authorize] // ОБ-1: только авторизованные пользователи
public class TicketsController(TicketService ticketService, AppDbContext db, AuditService audit) : ControllerBase
{
    // GET /api/tickets
    // FR-17: список с фильтрами по статусу, исполнителю, приоритету, категории
    // FR-19: поиск через параметр search
    // ОБ-6: пагинация через page и pageSize
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? assigneeId,
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? slaBreached,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null)
    {
        // ОБ-8: заявитель видит только свои тикеты — запрет доступа к чужим данным
        var userId = User.GetUserId();
        var role   = User.GetRole();

        var result = await ticketService.GetListAsync(
            status, priority, assigneeId, categoryId, search, slaBreached,
            page, pageSize, sortBy, sortDir, userId, role);
        return Ok(result);
    }

    // GET /api/tickets/my
    // FR-18: для заявителя - созданные им, для исполнителя - назначенные ему
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var items = await ticketService.GetMyAsync(userId, role);
        return Ok(new { items, total = items.Count() });
    }

    // POST /api/tickets
    // FR-1: создание обращения (тема, описание, категория, приоритет)
    // ОБ-4: валидация через атрибуты на CreateTicketRequest
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Тема обязательна" });

        var userId = User.GetUserId();
        var username = User.GetUsername();

        try
        {
            var ticket = await ticketService.CreateAsync(req, userId, username);
            return StatusCode(201, ticket);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/tickets/{id}
    // возвращает тикет вместе с комментариями и историей изменений
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();

        try
        {
            var ticket = await ticketService.GetByIdAsync(id, userId, role);
            return Ok(ticket);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // PATCH /api/tickets/{id}/status
    // FR-6, FR-7: смена статуса - проверяем таблицу переходов
    // FR-8: reopen - переход из resolved/closed в in_progress
    // FR-23: нельзя закрыть если есть открытые блокирующие тикеты
    // FR-24: при переходе в waiting - пауза SLA
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest req)
    {
        var userId = User.GetUserId();
        var username = User.GetUsername();
        var isAdmin = User.GetRole() == "admin"; // FR-22: admin может менять принудительно

        try
        {
            var ticket = await ticketService.ChangeStatusAsync(id, req, userId, username, isAdmin);
            return Ok(ticket);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "INVALID_TRANSITION")
        {
            // FR-7: недопустимый переход - возвращаем 409
            return Conflict(new { error = "Недопустимый переход статуса" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "BLOCKED_BY_OPEN")
        {
            // FR-23: нельзя закрыть пока есть незакрытые блокирующие
            return Conflict(new { error = "Нельзя закрыть тикет: есть открытые блокирующие обращения" });
        }
    }

    // PATCH /api/tickets/{id}/assignee
    // FR-10: назначение и переназначение исполнителя
    // ОБ-2: только исполнитель или администратор могут назначать
    [HttpPatch("{id:int}/assignee")]
    [Authorize(Roles = "executor,admin")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssigneeRequest req)
    {
        var userId = User.GetUserId();
        var username = User.GetUsername();

        try
        {
            var ticket = await ticketService.AssignAsync(id, req.AssigneeId, userId, username);
            return Ok(ticket);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // POST /api/tickets/bulk
    // FR-13: массовые операции над выборкой (смена статуса или исполнителя)
    // ОБ-2: только исполнитель или администратор
    [HttpPost("bulk")]
    [Authorize(Roles = "executor,admin")]
    public async Task<IActionResult> Bulk([FromBody] BulkRequest req)
    {
        var userId = User.GetUserId();
        var username = User.GetUsername();
        var updated = await ticketService.BulkAsync(req.Ids, req.Action, req.Value, userId, username);
        return Ok(new { updated });
    }

    // POST /api/tickets/{id}/dependencies
    // FR-23: добавить блокировку (этот тикет заблокирован тем который указан)
    [HttpPost("{id:int}/dependencies")]
    [Authorize(Roles = "executor,admin")]
    public async Task<IActionResult> AddDependency(int id, [FromBody] DependencyRequest req)
    {
        // тикет не может блокировать сам себя
        if (id == req.BlockedById)
            return BadRequest(new { error = "Тикет не может блокировать сам себя" });

        var exists = await db.TicketDependencies
            .AnyAsync(d => d.TicketId == id && d.BlockedById == req.BlockedById);
        if (exists)
            return Conflict(new { error = "Зависимость уже существует" });

        db.TicketDependencies.Add(new TicketDependency
        {
            TicketId    = id,
            BlockedById = req.BlockedById
        });

        // FR-12: фиксируем добавление блокировки в историю
        db.HistoryEntries.Add(new HistoryEntry
        {
            TicketId  = id,
            AuthorId  = User.GetUserId(),
            Field     = "dependency",
            NewValue  = $"blocker:{req.BlockedById}",
            CreatedAt = DateTime.UtcNow
        });

        audit.Log(User.GetUserId(), User.GetUsername(), "add_dependency", "ticket", id.ToString());
        await db.SaveChangesAsync();

        return StatusCode(201, new { ticketId = id, blockedById = req.BlockedById });
    }

    // DELETE /api/tickets/{id}/dependencies/{blockedById}
    // FR-23: удалить блокировку
    [HttpDelete("{id:int}/dependencies/{blockedById:int}")]
    [Authorize(Roles = "executor,admin")]
    public async Task<IActionResult> RemoveDependency(int id, int blockedById)
    {
        var dep = await db.TicketDependencies
            .FirstOrDefaultAsync(d => d.TicketId == id && d.BlockedById == blockedById);
        if (dep is null) return NotFound(new { error = "Зависимость не найдена" });

        db.TicketDependencies.Remove(dep);
        audit.Log(User.GetUserId(), User.GetUsername(), "remove_dependency", "ticket", id.ToString());
        await db.SaveChangesAsync();

        return Ok(new { deleted = true });
    }
}
