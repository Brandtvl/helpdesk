namespace Helpdesk.Api.Models;

// FR-23: блокирует / блокируется
public class TicketDependency
{
    public int TicketId { get; set; }       // тикет, который заблокирован
    public Ticket Ticket { get; set; } = null!;
    public int BlockedById { get; set; }    // тикет, который блокирует
    public Ticket BlockedByTicket { get; set; } = null!;
}
