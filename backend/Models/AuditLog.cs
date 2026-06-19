namespace Helpdesk.Api.Models;

// ОБ-7: журналирование значимых действий
public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
