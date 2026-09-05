using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs.Tickets;

public class TicketQueryParameters
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketStatus? Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketPriority? Priority { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
