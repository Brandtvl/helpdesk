using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Api.DTOs;

public record CreateTicketRequest(
    [Required, MinLength(3), MaxLength(200)] string Title,
    [Required, MaxLength(5000)] string Description,
    [Range(1, int.MaxValue)] int CategoryId,
    [Required] string Priority
);

public record ChangeStatusRequest(
    [Required] string Status,
    [MaxLength(1000)] string? Comment
);

public record AssigneeRequest(int? AssigneeId);

public record BulkRequest(
    [Required, MinLength(1)] int[] Ids,
    [Required] string Action,
    [Required] string Value
);

public record TicketDto(
    int Id,
    string Number,
    string Title,
    string Description,
    string Status,
    string Priority,
    int CategoryId,
    string? CategoryName,
    int AuthorId,
    string? AuthorName,
    int? AssigneeId,
    string? AssigneeName,
    string CreatedAt,
    string SlaDeadline,
    bool SlaBreached,
    bool SlaPaused,
    int[] BlockedByIds,
    int[] ChildIds
);

public record TicketDetailDto(
    int Id,
    string Number,
    string Title,
    string Description,
    string Status,
    string Priority,
    int CategoryId,
    string? CategoryName,
    int AuthorId,
    string? AuthorName,
    int? AssigneeId,
    string? AssigneeName,
    string CreatedAt,
    string SlaDeadline,
    bool SlaBreached,
    bool SlaPaused,
    int[] BlockedByIds,
    int[] ChildIds,
    IEnumerable<CommentDto> Comments,
    IEnumerable<HistoryEntryDto> History
);

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
