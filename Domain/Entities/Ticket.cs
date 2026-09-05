using Domain.Enums;

namespace Domain.Entities;

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? AssignedAgentUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    // SQL Server rowversion concurrency token
    public byte[] Version { get; set; } = null!;

    // Navigation properties
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ApplicationUser? AssignedAgent { get; set; }
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}
