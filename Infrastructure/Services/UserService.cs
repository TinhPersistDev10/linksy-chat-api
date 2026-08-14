using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Responses.Users;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class UserService : IUserService

    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly ICacheService _cache;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserService> _logger;

        public UserService(
           IUnitOfWork unitOfWork,
            IFileService fileService,
            ICacheService cache,
            INotificationService notificationService,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _cache = cache;
            _notificationService = notificationService;
            _logger = logger;
        }
        //GET USER CURRENT 
        public async Task<UserInfoDto> GetCurrentUserAsync(Guid userId)
        {
            try
            {
                var cacheKey = CacheKeys.UserProfile(userId);

                return await _cache.GetOrSetAsync(cacheKey, async () =>
                {
                    var user = await _unitOfWork.UserRepository.GetByIdAsNoTrackingAsync(userId)
                        ?? throw new KeyNotFoundException("User not found");

                    var roles = await _unitOfWork.UserRoles.Query()
                        .Where(ur => ur.UserId == userId)
                        .Include(ur => ur.Role)
                        .Select(ur => ur.Role.RoleName)
                        .ToListAsync();

                    return UserMapper.ToResponse(user, roles);
                }, CacheKeys.MediumTtl);

            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("User profile not found. UserId={userId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user profile. UserId = {userId}", userId);
                throw;
            }
        }
        //EDIT USER
        public async Task<UserInfoDto> UpdateUserAsync(Guid userId, UpdateUserByAdminDto updateUserDto)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

            if (!string.IsNullOrWhiteSpace(updateUserDto.Username)
                && updateUserDto.Username != user.Username)
            {
                ProfileValidationHelper.EnsureUsername(updateUserDto.Username);

                bool taken = await _unitOfWork.UserRepository.IsUsernameExistsAsync(
                    updateUserDto.Username.Trim(), userId);

                if (taken)
                    throw new InvalidOperationException("Username đã được sử dụng");

                user.Username = updateUserDto.Username.Trim();
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.Email)
                && updateUserDto.Email != user.Email)
            {
                bool taken = await _unitOfWork.UserRepository.IsEmailExistsAsync(updateUserDto.Email, userId);
                if (taken)
                    throw new InvalidOperationException("Email đã được sử dụng");
                user.Email = updateUserDto.Email.Trim();
                user.IsEmailVerified = false;
                user.EmailVerifiedAt = null;
            }
            // Nullable fields: only overwrite when caller supplies a value
            if (updateUserDto.Fullname is not null)
                user.Fullname = ProfileValidationHelper.NormalizeFullname(updateUserDto.Fullname);

            if (updateUserDto.Bio is not null)
                user.Bio = ProfileValidationHelper.NormalizeBio(updateUserDto.Bio);

            if (updateUserDto.DateOfBirth.HasValue)
            {
                ProfileValidationHelper.EnsureDateOfBirth(updateUserDto.DateOfBirth);
                user.DateOfBirth = updateUserDto.DateOfBirth.Value;
            }

            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            if (updateUserDto.IsEmailVerified.HasValue)
            {
                user.IsEmailVerified = updateUserDto.IsEmailVerified.Value;
                // Stamp verification time when an admin manually verifies the email
                if (updateUserDto.IsEmailVerified.Value && user.EmailVerifiedAt is null)
                    user.EmailVerifiedAt = DateTime.UtcNow;
            }

            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            try
            {
                await _cache.RemoveAsync(CacheKeys.UserProfile(userId));
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Profile updated but cache invalidation failed. UserId = {userId}, UpdateUser = {updateuserDto}", userId, updateUserDto);
                throw;
            }
            _logger.LogInformation(
                "User profile updated successfully. UserId={UserId}, UpdatedAt={UpdatedAt}",
                userId,
                user.UpdatedAt);
            return UserMapper.ToResponse(user);
        }
        //UPDATE AVATAR
        public async Task<AvatarResponse> UpdateUserAvatarAsync(Guid userId, IFormFile avatarFile)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new AvatarResponse { Success = false, Message = "User not found" };
                }

                // Xóa avatar cũ nếu có
                if (!string.IsNullOrEmpty(user.Avatar) && !DefaultAvatarHelper.IsDefaultAvatar(user.Avatar))
                {
                    await _fileService.DeleteAvatarAsync(user.Avatar);
                }

                // Upload avatar mới
                var avatarUrl = await _fileService.UploadAvatarAsync(avatarFile, "avatars/users");

                // Cập nhật database
                user.Avatar = avatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                await _cache.RemoveAsync(CacheKeys.UserProfile(userId));

                try
                {
                    await _notificationService.NotifyFriendAvatarChangedAsync(userId, avatarUrl);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(
                        notifyEx,
                        "Avatar updated but friend notifications failed. UserId={UserId}",
                        userId);
                }

                return new AvatarResponse
                {
                    Success = true,
                    Message = "Avatar updated successfully",
                    AvatarUrl = avatarUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user avatar for userId: {UserId}", userId);
                return new AvatarResponse { Success = false, Message = ex.Message };
            }
        }
        //DELETE AVATAR
        public async Task<AvatarResponse> DeleteUserAvatarAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new AvatarResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy người dùng"
                    };
                }

                // Xóa file avatar nếu không phải default
                if (!string.IsNullOrEmpty(user.Avatar) &&
                    !DefaultAvatarHelper.IsDefaultAvatar(user.Avatar))
                {
                    await _fileService.DeleteAvatarAsync(user.Avatar);
                }

                // Set về avatar mặc định
                user.Avatar = DefaultAvatarHelper.GetDefaultUserAvatar(userId, username: user.Username, fullname: user.Fullname);
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                await _cache.RemoveAsync(CacheKeys.UserProfile(userId));

                try
                {
                    await _notificationService.NotifyFriendAvatarChangedAsync(userId, user.Avatar);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(
                        notifyEx,
                        "Avatar reset but friend notifications failed. UserId={UserId}",
                        userId);
                }

                return new AvatarResponse
                {
                    Success = true,
                    Message = "Đã đặt lại avatar mặc định",
                    AvatarUrl = user.Avatar
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user avatar for userId: {UserId}", userId);
                return new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<List<UserLookupResponse>> SearchUsersAsync(Guid currentUserId, string query, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            try
            {
                var users = await _unitOfWork.UserRepository.SearchUsersAsync(query, currentUserId, limit);
                var result = users.Select(user => new UserLookupResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Fullname = user.Fullname,
                    Avatar = DefaultAvatarHelper.GetAvatarOrDefault(
                        user.Avatar,
                        user.UserId),
                    Bio = user.Bio

                }).ToList();
                _logger.LogInformation("User search completed. RequestedBy={UserId}, QueryLength={QueryLength}, Limit={Limit}, ResultCount={ResultCount}",
                    currentUserId,
                    query.Length,
                    limit,
                    result.Count);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(
                           ex,
                           "User search failed. RequestedBy={UserId}, QueryLength={QueryLength}, Limit={Limit}",
                           currentUserId,
                           query.Length,
                           limit
                           );

                throw;
            }
        }
    }
}