using Domain.Enums;

namespace Domain.Entities;

public class TicketHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public TicketStatus? FromStatus { get; set; }
    public TicketStatus? ToStatus { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Actor { get; set; } = null!;
}
