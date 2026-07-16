using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.DTOs;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);
        Task<User?> GetWithRolesAsync(Guid userId);
        Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null); // ✅ 1 method duy nhất
        Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null);
        Task<List<User>> GetOnlineUsersAsync(List<Guid> userIds);
        Task<List<User>> SearchUsersAsync(string searchTerm, Guid excludedUserId, int limit = 20);
        Task<List<Guid>> GetExistingUserIdsAsync(List<Guid> userIds);
        Task<User?> GetByIdAsNoTrackingAsync(Guid userId);

    }
}