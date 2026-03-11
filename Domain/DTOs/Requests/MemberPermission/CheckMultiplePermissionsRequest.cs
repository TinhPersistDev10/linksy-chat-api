 using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class CheckMultiplePermissionsRequest
    {
        [Required]
        public List<string> PermissionTypes { get; set; } = new();
    }
}
