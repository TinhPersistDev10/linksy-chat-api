using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class AddMembersRequest
    {
        [Required(ErrorMessage = "Danh sách thành viên không được để trống")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 thành viên")]
        public List<Guid> MemberIds { get; set; } = new();

    }
}