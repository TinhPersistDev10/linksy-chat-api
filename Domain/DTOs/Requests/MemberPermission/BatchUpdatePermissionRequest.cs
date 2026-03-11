namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class BatchUpdatePermissionRequest
    {
        public List<UserPermissionUpdate> Permissions { get; set; } = new();
    }
}
