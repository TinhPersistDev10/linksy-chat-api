using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class MuteMemberRequest
    {
        [Range(1, 525600, ErrorMessage = "DurationInMinutes phải từ 1 phút đến 1 năm")]
        public int? DurationInMinutes { get; set; }
    }
}
