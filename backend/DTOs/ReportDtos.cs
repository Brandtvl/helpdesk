namespace Helpdesk.Api.DTOs;

public record StatusCountDto(string Status, int Count);

public record AvgResolutionDto(double AvgHours);

public record ExecutorLoadDto(int ExecutorId, string Username, int Count);

public record DependencyRequest(int BlockedById);

public record AdminForceRequest(string? Status, int? AssigneeId, string Reason);

public record CategoryDto(int Id, string Name);

public record CreateCategoryRequest(string Name);

public record FileDto(int Id, string Filename, string Url);
