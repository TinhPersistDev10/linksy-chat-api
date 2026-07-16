using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class BatchUpdatePermissionRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one permission update is required.")]
        public List<UserPermissionUpdate> Permissions { get; set; } = new();
    }
}
