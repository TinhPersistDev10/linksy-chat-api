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
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<List<User>> GetOnlineUsersAsync(List<Guid> userIds);
        Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 20);
        
    }
}