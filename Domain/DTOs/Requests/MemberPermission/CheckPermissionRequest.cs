using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class CheckPermissionRequest
    {

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string PermissionType { get; set; } = string.Empty;
    }
}
