namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class UpdateMemberPermissionRequest
    {
        public bool? CanSendMessages { get; set; }
        public bool? CanSendVoice { get; set; }
        public bool? CanSendFiles { get; set; }
        public bool? CanSendMedia { get; set; }
        public bool? CanInviteMembers { get; set; }
        public bool? CanRemoveMembers { get; set; }
        public bool? CanDeleteMessages { get; set; }
        public bool? CanEditGroupInfo { get; set; }
        public bool? CanPinMessages { get; set; }
        public bool? CanManageCalls { get; set; }

    }
}
