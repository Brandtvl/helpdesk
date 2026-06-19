using Helpdesk.Api.Enums;

namespace Helpdesk.Api.Services;

// FR-6: фиксированный набор статусов (new, in_progress, waiting, resolved, closed)
// FR-7: таблица переходов - сервер отклоняет недопустимые переходы (409)
// FR-8: повторное открытие - из resolved и closed можно вернуть в работу
public static class TicketStateMachine
{
    // тут хранится таблица допустимых переходов
    // ключ - текущий статус, значение - куда можно перейти
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            { TicketStatus.New,        new[] { TicketStatus.InProgress } },
            { TicketStatus.InProgress, new[] { TicketStatus.Waiting, TicketStatus.Resolved } },
            { TicketStatus.Waiting,    new[] { TicketStatus.InProgress } },
            { TicketStatus.Resolved,   new[] { TicketStatus.Closed, TicketStatus.InProgress } }, // FR-8
            { TicketStatus.Closed,     new[] { TicketStatus.InProgress } },                       // FR-8
        };

    // FR-7: возвращает true если переход разрешён
    public static bool CanTransition(TicketStatus from, TicketStatus to)
    {
        if (!AllowedTransitions.TryGetValue(from, out var allowed))
            return false;

        return allowed.Contains(to);
    }

    // парсим строку из JSON в enum статуса
    public static TicketStatus Parse(string value)
    {
        switch (value.ToLowerInvariant())
        {
            case "new":         return TicketStatus.New;
            case "in_progress": return TicketStatus.InProgress;
            case "waiting":     return TicketStatus.Waiting;
            case "resolved":    return TicketStatus.Resolved;
            case "closed":      return TicketStatus.Closed;
            default:
                throw new ArgumentException($"Неизвестный статус: {value}");
        }
    }

    // обратно - enum в строку для ответа фронтенду
    public static string Serialize(TicketStatus status)
    {
        switch (status)
        {
            case TicketStatus.New:        return "new";
            case TicketStatus.InProgress: return "in_progress";
            case TicketStatus.Waiting:    return "waiting";
            case TicketStatus.Resolved:   return "resolved";
            case TicketStatus.Closed:     return "closed";
            default:
                return status.ToString().ToLowerInvariant();
        }
    }
}
