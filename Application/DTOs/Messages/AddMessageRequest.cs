using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Messages;

public class AddMessageRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}
