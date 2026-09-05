using System.Text.Json.Serialization;

namespace Application.DTOs.History;

public class HistoryResponse
{
    public int Id { get; set; }
    public string ActorEmail { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.EventType EventType { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketStatus? FromStatus { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketStatus? ToStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
