namespace linksy_backend_api.Domain.DTOs.Responses.MemberPermission
{
    public class PermissionCheckResponse
    {
        public bool HasPermission { get; set; }
        public string PermissionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
