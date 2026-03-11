using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MemberPermission
    {
        public Guid PermissionId { get; set; }
        public Guid MemberId { get; set; }
        public bool CanSendMessages { get; set; } = true;
        public bool CanSendMedia { get; set; } = true;
        public bool CanSendVoice { get; set; } = true;
        public bool CanSendFiles { get; set; } = true;
        public bool CanInviteMembers { get; set; }
        public bool CanRemoveMembers { get; set; }
        public bool CanEditGroupInfo { get; set; }
        public bool CanPinMessages { get; set; }
        public bool CanDeleteMessages { get; set; }
        public bool CanManageCalls { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public virtual ChatroomMember Member { get; set; } = null!;
    }
}
