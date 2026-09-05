namespace Application.DTOs.Messages;

public class MessageResponse
{
    public int Id { get; set; }
    public string AuthorEmail { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
