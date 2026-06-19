namespace Helpdesk.Api.Models;

public class TicketFile
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string Filename { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
