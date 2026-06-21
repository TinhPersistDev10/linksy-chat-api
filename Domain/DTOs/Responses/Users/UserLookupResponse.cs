namespace linksy_backend_api.Domain.DTOs.Responses.Users;

public class UserLookupResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Fullname { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
}