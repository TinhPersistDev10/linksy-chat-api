namespace linksy_backend_api.Domain.DTOs.Responses.MemberPermission
{
    public class MemberPermissionResponse
    {
        public Guid PermissionId { get; set; }
        public Guid MemberId { get; set; }

        // User info
        public Guid UserId { get; set; }
        public required string UserName { get; set; }
        public string? UserAvatar { get; set; }
        public required string Email { get; set; }

        // Chatroom info
        public Guid ChatroomId { get; set; }
        public string ChatroomName { get; set; } = string.Empty;

        // Message permissions
        public bool CanSendMessages { get; set; }
        public bool CanSendMedia { get; set; }
        public bool CanSendVoice { get; set; }
        public bool CanSendFiles { get; set; }
        public bool CanDeleteMessages { get; set; }
        public bool CanPinMessages { get; set; }

        // Member management
        public bool CanInviteMembers { get; set; }
        public bool CanRemoveMembers { get; set; }

        // Group management
        public bool CanEditGroupInfo { get; set; }
        public bool CanManageCalls { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
