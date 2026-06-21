using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Domain.DTOs.Responses.Users;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Models;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserInfoDto> GetCurrentUserAsync(Guid userId);

        Task<UserInfoDto> UpdateUserAsync(Guid userId, UpdateUserByAdminDto updateUserDto);
        Task<AvatarResponse> UpdateUserAvatarAsync(Guid userId, IFormFile avatarFile);
        Task<AvatarResponse> DeleteUserAvatarAsync(Guid userId);
        Task<List<UserLookupResponse>> SearchUsersAsync(Guid currentUserId, string query, int limit = 20);
    }
}