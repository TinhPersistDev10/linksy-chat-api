using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class ChatroomSettingsResponseDto
    {
        public bool AllowMemberInvite { get; set; }
        public bool AllowMemberLeave { get; set; }
        public int MaxMembers { get; set; }
        public bool RequireApprovalToJoin { get; set; }
    }
}