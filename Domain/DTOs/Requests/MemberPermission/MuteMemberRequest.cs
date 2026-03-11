namespace linksy_backend_api.Domain.DTOs.Requests.MemberPermission
{
    public class MuteMemberRequest
    {
        public int? DurationInMinutes { get; set; } // null = vĩnh viễn
    }
}
