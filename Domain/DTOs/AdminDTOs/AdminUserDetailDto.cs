using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class AdminUserDetailDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Fullname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? AccountLockedUntil { get; set; }
        public List<RoleDto> Roles { get; set; } = new();
        public int MessageCount { get; set; }
        public int FriendCount { get; set; }

        public string ModerationLevel { get; set; } = "none";
        public string? ModerationReason { get; set; }
        public DateTime? ModerationExpiresAt { get; set; }
        public DateTime? ModeratedAt { get; set; }
        public int ViolationPoints { get; set; }
        public bool IsFlaggedForReview { get; set; }
    }
}