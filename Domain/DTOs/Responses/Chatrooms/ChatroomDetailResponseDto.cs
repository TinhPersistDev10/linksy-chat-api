using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class ChatroomDetailResponseDto
    {
        public Guid ChatroomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        
        public List<ChatroomMemberDetailResponseDto> Members { get; set; } = new();
        public ChatroomSettingsResponseDto Settings { get; set; } = new();
        public ChatroomStatisticsResponseDto Statistics { get; set; } = new();
    }
}