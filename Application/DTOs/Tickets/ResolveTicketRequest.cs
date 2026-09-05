using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tickets;

public class ResolveTicketRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string ResolutionMessage { get; set; } = string.Empty;
}
