using linksy_backend_api.Domain.DTOs.Requests.Settings;
using linksy_backend_api.Domain.DTOs.Responses.Settings;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Repositories.IRepositories;

namespace linksy_backend_api.Infrastructure.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserSettingsService> _logger;
        private readonly ICacheService _cache;

        public UserSettingsService(
            IUnitOfWork unitOfWork,
            ILogger<UserSettingsService> logger,
            ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cache = cache;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ALL
        // ─────────────────────────────────────────────────────────────────────

        public async Task<AllSettingsResponse> GetAllSettingsAsync(Guid userId)
        {
            var userSettings = await _unitOfWork.UserSettingsRepository.GetOrCreateAsync(userId);
            var notificationSettings = await _unitOfWork.NotificationSettingsRepository.GetOrCreateAsync(userId);
            var privacySettings = await _unitOfWork.PrivacySettingsRepository.GetOrCreateAsync(userId);
            var userStatus = await _unitOfWork.UserStatusRepository.GetOrCreateAsync(userId);

            // Persist newly created defaults
            await _unitOfWork.SaveChangesAsync();

            return new AllSettingsResponse
            {
                UserSettings = new UserSettingsResponse
                {
                    SettingId = userSettings.SettingId,
                    Language = userSettings.Language,
                    Timezone = userSettings.Timezone,
                    Theme = userSettings.Theme,
                    CreatedAt = userSettings.CreatedAt,
                    UpdatedAt = userSettings.UpdatedAt
                },
                NotificationSettings = new NotificationSettingsResponse
                {
                    Id = notificationSettings.Id,
                    NotificationsEnabled = notificationSettings.NotificationsEnabled,
                    NotificationSoundEnabled = notificationSettings.NotificationSoundEnabled,
                    MessagePreviewEnabled = notificationSettings.MessagePreviewEnabled,
                    EmailNotifications = notificationSettings.EmailNotifications
                },
                PrivacySettings = new PrivacySettingsResponse
                {
                    Id = privacySettings.Id,
                    ReadReceiptsEnabled = privacySettings.ReadReceiptsEnabled,
                    TypingIndicatorsEnabled = privacySettings.TypingIndicatorsEnabled,
                    LastSeenEnabled = privacySettings.LastSeenEnabled,
                    ProfilePhotoVisibility = privacySettings.ProfilePhotoVisibility,
                    StatusVisibility = privacySettings.StatusVisibility,
                    WhoCanAddToGroups = privacySettings.WhoCanAddToGroups
                },
                UserStatus = new UserStatusResponse
                {
                    StatusId = userStatus.StatusId,
                    StatusType = userStatus.StatusType,
                    CustomStatus = userStatus.CustomStatus,
                    LastSeenAt = userStatus.LastSeenAt,
                    UpdatedAt = userStatus.UpdatedAt
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE USER SETTINGS
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsRequest request)
        {
            try
            {
                var settings = await _unitOfWork.UserSettingsRepository.GetOrCreateAsync(userId);

                if (request.Language is not null) settings.Language = request.Language;
                if (request.Timezone is not null) settings.Timezone = request.Timezone;
                if (request.Theme is not null) settings.Theme = request.Theme;
                settings.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.UserSettingsRepo.Update(settings);
                await _unitOfWork.SaveChangesAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Cài đặt đã được cập nhật.",
                    Data = new UserSettingsResponse
                    {
                        SettingId = settings.SettingId,
                        Language = settings.Language,
                        Timezone = settings.Timezone,
                        Theme = settings.Theme,
                        CreatedAt = settings.CreatedAt,
                        UpdatedAt = settings.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings for {UserId}", userId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE NOTIFICATION SETTINGS
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> UpdateNotificationSettingsAsync(
            Guid userId,
            UpdateNotificationSettingsRequest request)
        {
            try
            {
                var settings = await _unitOfWork.NotificationSettingsRepository.GetOrCreateAsync(userId);

                if (request.NotificationsEnabled is not null)
                    settings.NotificationsEnabled = request.NotificationsEnabled.Value;
                if (request.NotificationSoundEnabled is not null)
                    settings.NotificationSoundEnabled = request.NotificationSoundEnabled.Value;
                if (request.MessagePreviewEnabled is not null)
                    settings.MessagePreviewEnabled = request.MessagePreviewEnabled.Value;
                if (request.EmailNotifications is not null)
                    settings.EmailNotifications = request.EmailNotifications.Value;

                _unitOfWork.NotificationSettingsRepo.Update(settings);
                await _unitOfWork.SaveChangesAsync();
                await _cache.RemoveAsync(CacheKeys.UserNotifSettings(userId));

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Cài đặt thông báo đã được cập nhật.",
                    Data = new NotificationSettingsResponse
                    {
                        Id = settings.Id,
                        NotificationsEnabled = settings.NotificationsEnabled,
                        NotificationSoundEnabled = settings.NotificationSoundEnabled,
                        MessagePreviewEnabled = settings.MessagePreviewEnabled,
                        EmailNotifications = settings.EmailNotifications
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings for {UserId}", userId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE PRIVACY SETTINGS
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> UpdatePrivacySettingsAsync(
            Guid userId,
            UpdatePrivacySettingsRequest request)
        {
            try
            {
                var settings = await _unitOfWork.PrivacySettingsRepository.GetOrCreateAsync(userId);

                if (request.ReadReceiptsEnabled is not null)
                    settings.ReadReceiptsEnabled = request.ReadReceiptsEnabled.Value;
                if (request.TypingIndicatorsEnabled is not null)
                    settings.TypingIndicatorsEnabled = request.TypingIndicatorsEnabled.Value;
                if (request.LastSeenEnabled is not null)
                    settings.LastSeenEnabled = request.LastSeenEnabled.Value;
                if (request.ProfilePhotoVisibility is not null)
                    settings.ProfilePhotoVisibility = request.ProfilePhotoVisibility;
                if (request.StatusVisibility is not null)
                    settings.StatusVisibility = request.StatusVisibility;
                if (request.WhoCanAddToGroups is not null)
                    settings.WhoCanAddToGroups = request.WhoCanAddToGroups;

                _unitOfWork.PrivacySettingsRepo.Update(settings);
                await _unitOfWork.SaveChangesAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Cài đặt quyền riêng tư đã được cập nhật.",
                    Data = new PrivacySettingsResponse
                    {
                        Id = settings.Id,
                        ReadReceiptsEnabled = settings.ReadReceiptsEnabled,
                        TypingIndicatorsEnabled = settings.TypingIndicatorsEnabled,
                        LastSeenEnabled = settings.LastSeenEnabled,
                        ProfilePhotoVisibility = settings.ProfilePhotoVisibility,
                        StatusVisibility = settings.StatusVisibility,
                        WhoCanAddToGroups = settings.WhoCanAddToGroups
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy settings for {UserId}", userId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE USER STATUS
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request)
        {
            try
            {
                var status = await _unitOfWork.UserStatusRepository.GetOrCreateAsync(userId);

                status.StatusType = request.StatusType;
                status.CustomStatus = request.CustomStatus;
                status.UpdatedAt = DateTime.UtcNow;

                if (request.StatusType == "offline")
                    status.LastSeenAt = DateTime.UtcNow;

                _unitOfWork.UserStatusRepo.Update(status);
                await _unitOfWork.SaveChangesAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Trạng thái đã được cập nhật.",
                    Data = new UserStatusResponse
                    {
                        StatusId = status.StatusId,
                        StatusType = status.StatusType,
                        CustomStatus = status.CustomStatus,
                        LastSeenAt = status.LastSeenAt,
                        UpdatedAt = status.UpdatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for {UserId}", userId);
                throw;
            }
        }
    }
}