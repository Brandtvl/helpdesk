using Helpdesk.Api.Data;
using Helpdesk.Api.Enums;
using Helpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Services;

// FR-14: нормативы реакции и решения задаются для каждого приоритета
// FR-15: расчёт сроков от момента регистрации, отображение оставшегося времени
// FR-16: пометка нарушения SLA - обращения с превышенным сроком выделяются
// FR-24: приостановка SLA на время статуса "ожидание ответа"
public class SlaService
{
    private readonly AppDbContext _db;

    public SlaService(AppDbContext db)
    {
        _db = db;
    }

    // FR-15: считаем дедлайн - берём норматив для приоритета и прибавляем к дате создания
    public async Task<DateTime> CalculateDeadlineAsync(Priority priority, DateTime createdAt)
    {
        var config = await _db.SlaConfigs.FirstOrDefaultAsync(s => s.Priority == priority);

        // если в базе нет норматива - используем дефолтные значения
        if (config == null)
            config = GetDefault(priority);

        return createdAt.AddHours(config.ResolutionHours);
    }

    // FR-24: ставим SLA на паузу когда тикет переходит в "ожидание ответа"
    // сохраняем время начала паузы чтобы потом посчитать сколько простоял
    public void PauseSla(Ticket ticket)
    {
        if (ticket.SlaPaused)
            return; // уже на паузе, ничего не делаем

        ticket.SlaPaused = true;
        ticket.SlaPausedAt = DateTime.UtcNow;
    }

    // FR-24: снимаем с паузы и сдвигаем дедлайн вперёд на время паузы
    // иначе несправедливо - время пока ждёшь ответ не должно идти в счёт SLA
    public void ResumeSla(Ticket ticket)
    {
        if (!ticket.SlaPaused || ticket.SlaPausedAt == null)
            return;

        var pausedDuration = DateTime.UtcNow - ticket.SlaPausedAt.Value;
        ticket.SlaPausedTotal += pausedDuration;
        ticket.SlaDeadline = ticket.SlaDeadline.Add(pausedDuration);
        ticket.SlaPaused = false;
        ticket.SlaPausedAt = null;
    }

    // FR-16: проверяем нарушен ли SLA и проставляем флаг на тикете
    public void CheckBreach(Ticket ticket)
    {
        // решённые и закрытые не считаются нарушением
        if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
        {
            ticket.SlaBreached = false;
            return;
        }

        // FR-24: если сейчас на паузе - берём время начала паузы как "текущее",
        // чтобы счётчик не шёл пока ждём ответа от пользователя
        DateTime effectiveNow;
        if (ticket.SlaPaused && ticket.SlaPausedAt.HasValue)
            effectiveNow = ticket.SlaPausedAt.Value;
        else
            effectiveNow = DateTime.UtcNow;

        ticket.SlaBreached = effectiveNow > ticket.SlaDeadline;
    }

    // дефолтные нормативы - используются если в базе нет записи
    // FR-14: low=72ч, medium=24ч, high=8ч, critical=4ч
    private static SlaConfig GetDefault(Priority priority)
    {
        if (priority == Priority.Critical)
            return new SlaConfig { Priority = Priority.Critical, ReactionHours = 0, ResolutionHours = 4 };

        if (priority == Priority.High)
            return new SlaConfig { Priority = Priority.High, ReactionHours = 2, ResolutionHours = 8 };

        if (priority == Priority.Medium)
            return new SlaConfig { Priority = Priority.Medium, ReactionHours = 4, ResolutionHours = 24 };

        return new SlaConfig { Priority = Priority.Low, ReactionHours = 8, ResolutionHours = 72 };
    }
}
