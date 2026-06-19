using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Enums;
using Helpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Services;

// FR-1:  создание обращения (тема, описание, категория, приоритет)
// FR-5:  уникальный человекочитаемый номер
// FR-9:  фиксация автора, времени и комментария при каждом переходе
// FR-10: назначение исполнителя с историей переназначений
// FR-12: полная история изменений
// FR-13: массовые операции над выборкой
// FR-17: список с фильтрами и пагинацией
// FR-18: раздел "мои обращения"
// FR-19: полнотекстовый поиск по теме и описанию
// FR-23: зависимости (блокирует/блокируется)
// FR-24: пауза SLA при переходе в "ожидание ответа"
public class TicketService
{
    private readonly AppDbContext _db;
    private readonly SlaService _sla;
    private readonly AuditService _audit;

    public TicketService(AppDbContext db, SlaService sla, AuditService audit)
    {
        _db = db;
        _sla = sla;
        _audit = audit;
    }

    // FR-1: создаём обращение
    // FR-5: номер формируется из Id в базе - так он всегда уникален и не сбрасывается
    public async Task<TicketDto> CreateAsync(CreateTicketRequest req, int authorId, string authorName)
    {
        var priority = ParsePriority(req.Priority);
        var now = DateTime.UtcNow;

        // FR-15: сразу считаем дедлайн SLA при создании
        var deadline = await _sla.CalculateDeadlineAsync(priority, now);

        var ticket = new Ticket
        {
            Number      = string.Empty, // заполним после сохранения, когда узнаем Id
            Title       = req.Title.Trim(),
            Description = req.Description.Trim(),
            CategoryId  = req.CategoryId,
            Priority    = priority,
            AuthorId    = authorId,
            Status      = TicketStatus.New,
            CreatedAt   = now,
            SlaDeadline = deadline
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // FR-5: формируем читаемый номер вида #42 из реального Id
        ticket.Number = "#" + ticket.Id;

        // ОБ-7: журналируем создание обращения
        _audit.Log(authorId, authorName, "create", "ticket", ticket.Id.ToString());
        await _db.SaveChangesAsync();

        return await MapToDto(ticket.Id);
    }

    // FR-6, FR-7: смена статуса с проверкой таблицы переходов
    // FR-9: записываем в историю кто, когда и с каким комментарием поменял статус
    // FR-23: нельзя закрыть тикет пока открыты блокирующие
    // FR-24: включаем/выключаем паузу SLA
    public async Task<TicketDto> ChangeStatusAsync(int ticketId, ChangeStatusRequest req, int userId, string username, bool isAdmin = false)
    {
        var ticket = await _db.Tickets
            .Include(t => t.BlockedBy).ThenInclude(d => d.BlockedByTicket)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new KeyNotFoundException("Тикет не найден");

        var newStatus = TicketStateMachine.Parse(req.Status);
        var oldStatus = ticket.Status;

        // FR-7: проверяем допустимость перехода (кроме принудительной смены администратором)
        if (!isAdmin && !TicketStateMachine.CanTransition(oldStatus, newStatus))
            throw new InvalidOperationException("INVALID_TRANSITION");

        // FR-23: нельзя закрыть если есть незакрытые блокирующие обращения
        if (newStatus == TicketStatus.Closed && !isAdmin)
        {
            bool hasOpenBlockers = false;
            foreach (var dep in ticket.BlockedBy)
            {
                var s = dep.BlockedByTicket.Status;
                if (s != TicketStatus.Closed && s != TicketStatus.Resolved)
                {
                    hasOpenBlockers = true;
                    break;
                }
            }

            if (hasOpenBlockers)
                throw new InvalidOperationException("BLOCKED_BY_OPEN");
        }

        // FR-24: при переходе в "ожидание" - ставим SLA на паузу
        // при выходе из "ожидания" - снимаем и сдвигаем дедлайн
        if (newStatus == TicketStatus.Waiting)
        {
            _sla.PauseSla(ticket);
        }
        else if (oldStatus == TicketStatus.Waiting && newStatus != TicketStatus.Waiting)
        {
            _sla.ResumeSla(ticket);
        }

        ticket.Status = newStatus;

        // FR-16: обновляем флаг нарушения SLA
        _sla.CheckBreach(ticket);

        // FR-9: записываем переход в историю изменений
        _db.HistoryEntries.Add(new HistoryEntry
        {
            TicketId  = ticket.Id,
            AuthorId  = userId,
            Field     = "status",
            OldValue  = TicketStateMachine.Serialize(oldStatus),
            NewValue  = TicketStateMachine.Serialize(newStatus),
            Comment   = req.Comment,
            CreatedAt = DateTime.UtcNow
        });

        // ОБ-7: журналируем смену статуса
        _audit.Log(userId, username, "status:" + TicketStateMachine.Serialize(newStatus), "ticket", ticketId.ToString());
        await _db.SaveChangesAsync();

        return await MapToDto(ticketId);
    }

    // FR-10: назначение/переназначение исполнителя с сохранением истории
    public async Task<TicketDto> AssignAsync(int ticketId, int? assigneeId, int userId, string username)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket == null)
            throw new KeyNotFoundException("Тикет не найден");

        var oldAssigneeId = ticket.AssigneeId;
        ticket.AssigneeId = assigneeId;

        // FR-12: записываем изменение исполнителя в историю
        _db.HistoryEntries.Add(new HistoryEntry
        {
            TicketId  = ticket.Id,
            AuthorId  = userId,
            Field     = "assignee",
            OldValue  = oldAssigneeId.HasValue ? oldAssigneeId.Value.ToString() : null,
            NewValue  = assigneeId.HasValue ? assigneeId.Value.ToString() : "null",
            CreatedAt = DateTime.UtcNow
        });

        _audit.Log(userId, username, "assign", "ticket", ticketId.ToString());
        await _db.SaveChangesAsync();

        return await MapToDto(ticketId);
    }

    // FR-17: список обращений с фильтрами по статусу, исполнителю, приоритету, категории
    // FR-19: полнотекстовый поиск по теме и описанию
    // ОБ-6: постраничный вывод, сортировка, фильтрация
    public async Task<PagedResult<TicketDto>> GetListAsync(
        string? status, string? priority, int? assigneeId, int? categoryId,
        string? search, bool? slaBreached, int page, int pageSize,
        string? sortBy, string? sortDir)
    {
        var query = _db.Tickets
            .Include(t => t.Author)
            .Include(t => t.Assignee)
            .Include(t => t.Category)
            .Include(t => t.BlockedBy)
            .Include(t => t.Blocking)
            .AsQueryable();

        // применяем фильтры если они переданы
        if (!string.IsNullOrEmpty(status))
        {
            var s = TicketStateMachine.Parse(status);
            query = query.Where(t => t.Status == s);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            var p = ParsePriority(priority);
            query = query.Where(t => t.Priority == p);
        }

        if (assigneeId.HasValue)
            query = query.Where(t => t.AssigneeId == assigneeId);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);

        // FR-19: поиск по теме и описанию
        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

        // FR-16: фильтр просроченных по SLA
        if (slaBreached.HasValue)
            query = query.Where(t => t.SlaBreached == slaBreached.Value);

        // ОБ-6: сортировка
        var sb = sortBy?.ToLowerInvariant();
        var sd = sortDir?.ToLowerInvariant();

        if (sb == "priority" && sd == "desc")
            query = query.OrderByDescending(t => t.Priority);
        else if (sb == "priority")
            query = query.OrderBy(t => t.Priority);
        else if (sb == "sladeadline" && sd == "desc")
            query = query.OrderByDescending(t => t.SlaDeadline);
        else if (sb == "sladeadline")
            query = query.OrderBy(t => t.SlaDeadline);
        else if (sd == "asc")
            query = query.OrderBy(t => t.CreatedAt);
        else
            query = query.OrderByDescending(t => t.CreatedAt); // по умолчанию - новые сверху

        var total = await query.CountAsync();

        // ОБ-6: пагинация
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // FR-16: обновляем флаги SLA перед отдачей данных
        foreach (var t in items)
            _sla.CheckBreach(t);
        await _db.SaveChangesAsync();

        return new PagedResult<TicketDto>(items.Select(MapDto), total, page, pageSize);
    }

    // FR-18: "мои обращения" - для заявителя созданные, для исполнителя назначенные
    public async Task<IEnumerable<TicketDto>> GetMyAsync(int userId, string role)
    {
        var query = _db.Tickets
            .Include(t => t.Author)
            .Include(t => t.Assignee)
            .Include(t => t.Category)
            .Include(t => t.BlockedBy)
            .Include(t => t.Blocking)
            .AsQueryable();

        // FR-18: фильтруем в зависимости от роли
        if (role == "applicant")
            query = query.Where(t => t.AuthorId == userId);   // заявитель видит свои
        else
            query = query.Where(t => t.AssigneeId == userId); // исполнитель видит назначенные

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tickets.Select(MapDto);
    }

    // получаем тикет с полной информацией - комментарии и история
    public async Task<TicketDetailDto> GetByIdAsync(int ticketId, int userId, string role)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Author)
            .Include(t => t.Assignee)
            .Include(t => t.Category)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.History).ThenInclude(h => h.Author)
            .Include(t => t.BlockedBy)
            .Include(t => t.Blocking)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new KeyNotFoundException("Тикет не найден");

        _sla.CheckBreach(ticket);
        await _db.SaveChangesAsync();

        // FR-11: заявитель не видит внутренние комментарии
        var commentsList = new List<CommentDto>();
        foreach (var c in ticket.Comments.OrderBy(x => x.CreatedAt))
        {
            if (role == "applicant" && c.IsInternal)
                continue;

            commentsList.Add(new CommentDto(
                c.Id, c.TicketId, c.AuthorId, c.Author?.Username,
                c.Text, c.IsInternal, c.CreatedAt.ToString("O")));
        }

        // FR-12: полная история изменений
        var historyList = new List<HistoryEntryDto>();
        foreach (var h in ticket.History.OrderBy(x => x.CreatedAt))
        {
            historyList.Add(new HistoryEntryDto(
                h.Id, h.TicketId, h.AuthorId, h.Author?.Username,
                h.Field, h.OldValue, h.NewValue, h.Comment,
                h.CreatedAt.ToString("O")));
        }

        var dto = MapDto(ticket);
        return new TicketDetailDto(
            dto.Id, dto.Number, dto.Title, dto.Description, dto.Status, dto.Priority,
            dto.CategoryId, dto.CategoryName, dto.AuthorId, dto.AuthorName,
            dto.AssigneeId, dto.AssigneeName, dto.CreatedAt, dto.SlaDeadline,
            dto.SlaBreached, dto.SlaPaused, dto.BlockedByIds, dto.ChildIds,
            commentsList, historyList
        );
    }

    // FR-13: массовые операции - меняем статус или исполнителя у нескольких тикетов сразу
    // тикеты с недопустимыми переходами просто пропускаем
    public async Task<int> BulkAsync(int[] ids, string action, string value, int userId, string username)
    {
        var tickets = await _db.Tickets
            .Include(t => t.BlockedBy).ThenInclude(d => d.BlockedByTicket)
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();

        int updated = 0;
        foreach (var ticket in tickets)
        {
            try
            {
                if (action == "setStatus")
                {
                    var req = new ChangeStatusRequest(value, "Массовое изменение");
                    await ChangeStatusAsync(ticket.Id, req, userId, username);
                    updated++;
                }
                else if (action == "setAssignee")
                {
                    if (int.TryParse(value, out int aid))
                    {
                        await AssignAsync(ticket.Id, aid, userId, username);
                        updated++;
                    }
                }
            }
            catch
            {
                // если переход недопустим или ещё что-то - просто пропускаем
            }
        }

        return updated;
    }

    // --- вспомогательные методы ---

    private async Task<TicketDto> MapToDto(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Author)
            .Include(t => t.Assignee)
            .Include(t => t.Category)
            .Include(t => t.BlockedBy)
            .Include(t => t.Blocking)
            .FirstAsync(t => t.Id == id);

        return MapDto(ticket);
    }

    public static TicketDto MapDto(Ticket t)
    {
        var blockedByIds = t.BlockedBy.Select(d => d.BlockedById).ToArray();
        var childIds = t.Blocking.Select(d => d.TicketId).ToArray();

        return new TicketDto(
            t.Id,
            t.Number,
            t.Title,
            t.Description,
            TicketStateMachine.Serialize(t.Status),
            t.Priority.ToString().ToLowerInvariant(),
            t.CategoryId,
            t.Category?.Name,
            t.AuthorId,
            t.Author?.Username,
            t.AssigneeId,
            t.Assignee?.Username,
            t.CreatedAt.ToString("O"),
            t.SlaDeadline.ToString("O"),
            t.SlaBreached,
            t.SlaPaused,
            blockedByIds,
            childIds
        );
    }

    private static Priority ParsePriority(string value)
    {
        switch (value.ToLowerInvariant())
        {
            case "low":      return Priority.Low;
            case "medium":   return Priority.Medium;
            case "high":     return Priority.High;
            case "critical": return Priority.Critical;
            default:
                throw new ArgumentException($"Неизвестный приоритет: {value}");
        }
    }
}
