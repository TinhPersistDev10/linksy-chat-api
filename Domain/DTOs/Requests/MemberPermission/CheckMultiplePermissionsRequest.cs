 using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class CheckMultiplePermissionsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one permission type")]
        public List<string> PermissionTypes { get; set; } = new();
    }
}
