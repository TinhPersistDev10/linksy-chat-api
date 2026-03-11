using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class CreateGroupChatroomRequest
    {
        [Required(ErrorMessage = "Tên nhóm không được để trống")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tên nhóm phải từ 1-100 ký tự")]
        public string RoomName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự")]
        public string? Description { get; set; }

        public string? Avatar { get; set; }

        public List<Guid>? MemberIds { get; set; }
    }
}