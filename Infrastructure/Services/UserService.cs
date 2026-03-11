using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class UserService : IUserService
    {

        private readonly LinksyDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IFileService _fileService;
        public UserService(LinksyDbContext context, ILogger<UserService> logger, IFileService fileService)
        {
            _context = context;
            _logger = logger;
            _fileService = fileService;
        }

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

        // private readonly I

        public async Task<UserInfoDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return MapToUserInfoDto(user);
        }

        public async Task<UserInfoDto> UpdateUserAsync(Guid userId, UpdateUserByAdminDto updateUserDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);


            return MapToUserInfoDto(user);
        }

        public async Task<AvatarResponse> UpdateUserAvatarAsync(Guid userId, IFormFile avatarFile)
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

                // Xóa avatar cũ nếu có
                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    await _fileService.DeleteAvatarAsync(user.Avatar);
                }

                // Upload avatar mới
                var avatarUrl = await _fileService.UploadAvatarAsync(avatarFile, "avatars/users");

                // Cập nhật database
                user.Avatar = avatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new AvatarResponse
                {
                    Success = true,
                    Message = "Cập nhật avatar thành công",
                    AvatarUrl = avatarUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user avatar for userId: {UserId}", userId);
                return new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private UserInfoDto MapToUserInfoDto(User user)
        {
            return new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                Fullname = user.Fullname ?? string.Empty,
                Avatar = DefaultAvatarHelper.GetAvatarOrDefault(user.Avatar, user.UserId, username: user.Username, fullname: user.Fullname),
                Bio = user.Bio ?? string.Empty,
                DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                IsEmailVerified = user.IsEmailVerified ?? false,
                CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow
            };
        }
    }
}