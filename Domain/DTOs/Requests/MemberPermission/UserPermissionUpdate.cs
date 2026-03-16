using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class UserPermissionUpdate
    {
        [Required]
        public Guid UserId { get; set; }

       [Required(ErrorMessage = "Permissions is required")]
        public UpdateMemberPermissionRequest Permissions { get; set; } = new();
    }
}
