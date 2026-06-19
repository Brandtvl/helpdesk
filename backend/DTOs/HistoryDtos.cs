namespace Helpdesk.Api.DTOs;

public record HistoryEntryDto(
    int Id,
    int TicketId,
    int AuthorId,
    string? AuthorName,
    string Field,
    string? OldValue,
    string NewValue,
    string? Comment,
    string CreatedAt
);
