using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(LinksyDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
        {
            return await FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername);
        }


        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<User>> GetOnlineUsersAsync(List<Guid> userIds)
        {
            return await Query()
                .Where(u => userIds.Contains(u.UserId) && u.IsActive.GetValueOrDefault())
                .ToListAsync();
        }

        public async Task<User?> GetWithRolesAsync(Guid userId)
        {
            return await Query()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await AnyAsync(u => u.Username == username);
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 20)
        {
            return await Query()
                .Where(u => u.IsActive ?? false && 
                    (u.Username.Contains(searchTerm) || 
                     u.Fullname.Contains(searchTerm) ||
                     u.Email.Contains(searchTerm)))
                .Take(limit)
                .ToListAsync();
        }
    }
}