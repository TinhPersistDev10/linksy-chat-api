namespace linksy_backend_api.Domain.DTOs.Responses.MemberPermission
{
    public class MultiplePermissionCheckResponse
    {
        public Dictionary<string, bool> Permissions { get; set; } = new();
        public bool AllGranted { get; set; }
        public List<string> DeniedPermissions { get; set; } = new();
    }
}
