namespace Helpdesk.Api.DTOs;

public record CreateCommentRequest(string Text, bool IsInternal);

public record CommentDto(
    int Id,
    int TicketId,
    int AuthorId,
    string? AuthorName,
    string Text,
    bool IsInternal,
    string CreatedAt
);
