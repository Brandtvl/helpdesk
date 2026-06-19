namespace Helpdesk.Api.DTOs;

public record SlaDto(string Priority, int ReactionHours, int ResolutionHours);

public record UpdateSlaRequest(int ReactionHours, int ResolutionHours);
