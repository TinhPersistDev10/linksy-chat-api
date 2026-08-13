using System;

namespace linksy_backend_api.DTOs.Moderation;

public class UserModerationStatusDto
{
    public string Level { get; set; } = "none";
    public string LevelLabel { get; set; } = "Không";
    public string? Reason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public int ViolationPoints { get; set; }
    public bool IsFlaggedForReview { get; set; }
    public bool CanLogin { get; set; } = true;
    public bool CanSendMessages { get; set; } = true;
    public bool CanStartCalls { get; set; } = true;
}
