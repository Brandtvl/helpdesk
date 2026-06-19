using Helpdesk.Api.Enums;

namespace Helpdesk.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Ticket> CreatedTickets { get; set; } = [];
    public ICollection<Ticket> AssignedTickets { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<HistoryEntry> HistoryEntries { get; set; } = [];
}
