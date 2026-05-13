using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class UserService : IUserService
    {

        private readonly LinksyDbContext _context;
        private readonly IFileService _fileService;
        private readonly ICacheService _cache;       // ← thêm
        private readonly ILogger<UserService> _logger;

        public UserService(
            LinksyDbContext context,
            IFileService fileService,
            ICacheService cache,                     // ← thêm
            ILogger<UserService> logger)
        {
            _context = context;
            _fileService = fileService;
            _cache = cache;
            _logger = logger;
        }
        //GET USER CURRENT 
        public async Task<UserInfoDto> GetCurrentUserAsync(Guid userId)
        {
            var cacheKey = CacheKeys.UserProfile(userId);

            return await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                var user = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId)
                    ?? throw new KeyNotFoundException("User not found");

                return UserMapper.ToResponse(user);
            }, CacheKeys.MediumTtl);
        }
        //EDIT USER
        public async Task<UserInfoDto> UpdateUserAsync(Guid userId, UpdateUserByAdminDto updateUserDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("User not found");
            if (!string.IsNullOrWhiteSpace(updateUserDto.Username) && updateUserDto.Username != user.Username)
            {
                bool taken = await _context.Users.AnyAsync(u => u.Username == updateUserDto.Username && u.UserId != userId);
                if (taken) { throw new InvalidOperationException("User already taken"); }
                user.Username = updateUserDto.Username;
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                bool taken = await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.UserId != userId);
                if (taken) throw new InvalidOperationException("Email already taken");
                user.Email = updateUserDto.Email;
                user.IsEmailVerified = false;
                user.EmailVerifiedAt = null;
            }
            // Nullable fields: only overwrite when caller supplies a value
            if (updateUserDto.Fullname is not null)
                user.Fullname = updateUserDto.Fullname;

            if (updateUserDto.Bio is not null)
                user.Bio = updateUserDto.Bio;

            if (updateUserDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateUserDto.DateOfBirth.Value;

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

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} profile updated", userId);
            await _cache.RemoveAsync(CacheKeys.UserProfile(userId));
            return UserMapper.ToResponse(user);
        }
        //UPDATE AVATAR
        public async Task<AvatarResponse> UpdateUserAvatarAsync(Guid userId, IFormFile avatarFile)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
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

                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(CacheKeys.UserProfile(userId));
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
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
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
                await _context.SaveChangesAsync();

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
    }
}