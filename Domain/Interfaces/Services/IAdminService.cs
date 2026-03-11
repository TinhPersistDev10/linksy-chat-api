using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.DTOs;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IAdminService
    {
        // User Management
        Task<ApiResponseDto> GetAllUsersAsync(int page, int pageSize, string? searchTerm);
        Task<ApiResponseDto> GetUserDetailAsync(Guid userId);
        Task<ApiResponseDto> CreateUserAsync(CreateUserByAdminDto dto);
        Task<ApiResponseDto> UpdateUserAsync(Guid userId, UpdateUserByAdminDto dto);
        Task<ApiResponseDto> DeleteUserAsync(Guid userId);
        Task<ApiResponseDto> ToggleUserStatusAsync(Guid userId);
        Task<ApiResponseDto> ResetUserPasswordAsync(Guid userId, string newPassword);
        
        // Role Management
        Task<ApiResponseDto> AssignRoleAsync(AssignRoleDto dto);
        Task<ApiResponseDto> RemoveRoleAsync(Guid userId, int roleId);
        Task<ApiResponseDto> GetAllRolesAsync();
        Task<ApiResponseDto> GetUserRolesAsync(Guid userId);
        
        // Statistics
        Task<ApiResponseDto> GetStatisticsAsync();
        Task<ApiResponseDto> GetRecentActivitiesAsync(int limit);


        Task<ApiResponseDto> HardDeleteUserAsync(Guid userId);

    }
}