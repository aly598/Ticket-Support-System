using System.Text.Json.Serialization;

namespace Application.DTOs.Tickets;

public class TicketResponse
{
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketStatus Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketPriority Priority { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string? AssignedAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string Version { get; set; } = string.Empty;

    public List<Application.DTOs.Messages.MessageResponse>? Messages { get; set; }
}
