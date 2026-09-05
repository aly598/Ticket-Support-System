using Domain.Enums;

namespace Application.DTOs.Dashboard;

public class DashboardViewModel
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public List<DashboardTicketRow> Tickets { get; set; } = new();

    // Filter state
    public TicketStatus? FilterStatus { get; set; }
    public TicketPriority? FilterPriority { get; set; }
}

public class DashboardTicketRow
{
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string? AssignedAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
