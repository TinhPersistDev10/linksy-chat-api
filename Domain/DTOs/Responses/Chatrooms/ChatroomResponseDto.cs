using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.DTOs.Responses.Chatrooms;
using linksy_backend_api.DTOs.MessagesDTOs;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class ChatroomResponseDto
    {
        public Guid ChatroomId { get; set; }
        public string RoomName { get; set; } = string.Empty;    
        public string Description { get; set; }   = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public MessageResponse LastMessage { get; set; } = new();
        public List<ChatroomMemberRequest> Members { get; set; } = new();
        public int UnreadCount { get; set; }
        
        public MemberInfoResponseDto? MyMemberInfo {get;set;}

    }
}