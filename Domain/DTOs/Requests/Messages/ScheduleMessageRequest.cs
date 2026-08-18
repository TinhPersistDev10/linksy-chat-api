using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.MessagesDTOs;

public class ScheduleMessageRequest
{
    [Required]
    public Guid ChatroomId { get; set; }

    [Required]
    [StringLength(20)]
    public string MessageType { get; set; } = "text";

    [Required]
    [StringLength(5000)]
    public string MessageText { get; set; } = string.Empty;

    public Guid? ParentMessageId { get; set; }

    [Required]
    public DateTime SendAt { get; set; }
}
