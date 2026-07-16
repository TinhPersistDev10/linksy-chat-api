using linksy_backend_api.Domain.DTOs.Requests.Settings;
using linksy_backend_api.Domain.DTOs.Responses.Settings;
using linksy_backend_api.DTOs;

namespace linksy_backend_api.Domain.Interfaces.Services
{
    public interface IUserSettingsService
    {
        Task<AllSettingsResponse> GetAllSettingsAsync(Guid userId);

        Task<ApiResponseDto> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsRequest request);
        Task<ApiResponseDto> UpdateNotificationSettingsAsync(Guid userId, UpdateNotificationSettingsRequest request);
        Task<ApiResponseDto> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingsRequest request);
        Task<ApiResponseDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request);
    }
}