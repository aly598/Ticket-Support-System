namespace Domain.Entities;

public class TicketMessage
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
