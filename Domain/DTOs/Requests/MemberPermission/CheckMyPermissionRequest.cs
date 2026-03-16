using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class CheckMyPermissionRequest
    {
        
        [Required]
        [StringLength(50)]        
        public string PermissionType { get; set; } = string.Empty;
    }
}
