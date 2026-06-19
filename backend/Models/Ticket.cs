using Helpdesk.Api.Enums;

namespace Helpdesk.Api.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public Priority Priority { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SlaDeadline { get; set; }
    public bool SlaBreached { get; set; }

    // FR-24: SLA pause tracking
    public bool SlaPaused { get; set; }
    public DateTime? SlaPausedAt { get; set; }
    public TimeSpan SlaPausedTotal { get; set; } = TimeSpan.Zero;

    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<HistoryEntry> History { get; set; } = [];
    public ICollection<TicketFile> Files { get; set; } = [];

    // FR-23: dependencies
    public ICollection<TicketDependency> BlockedBy { get; set; } = [];
    public ICollection<TicketDependency> Blocking { get; set; } = [];
}
