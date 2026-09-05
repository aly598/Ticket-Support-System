using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs.Tickets;

public class CreateTicketRequest
{
    [Required]
    [StringLength(120, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 20)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Domain.Enums.TicketPriority Priority { get; set; }
}
