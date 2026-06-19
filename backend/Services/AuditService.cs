using Helpdesk.Api.Data;
using Helpdesk.Api.Models;

namespace Helpdesk.Api.Services;

// ОБ-7: журналирование значимых действий в системе
// записывает кто, что, с каким объектом и когда сделал
// используется во всех контроллерах при изменении данных
public class AuditService(AppDbContext db)
{
    public void Log(int? userId, string username, string action, string entity, string? entityId = null)
    {
        // добавляем запись в DbSet, SaveChanges вызывается в контроллере
        db.AuditLogs.Add(new AuditLog
        {
            UserId    = userId,
            Username  = username,
            Action    = action,    // например: "create_ticket", "change_status"
            Entity    = entity,    // тип объекта: "ticket", "category", "sla"
            EntityId  = entityId,  // id объекта (если есть)
            CreatedAt = DateTime.UtcNow
        });
    }
}
