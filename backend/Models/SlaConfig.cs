using Helpdesk.Api.Enums;

namespace Helpdesk.Api.Models;

public class SlaConfig
{
    public int Id { get; set; }
    public Priority Priority { get; set; }
    public int ReactionHours { get; set; }
    public int ResolutionHours { get; set; }
}
