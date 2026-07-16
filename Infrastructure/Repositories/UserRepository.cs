using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly LinksyDbContext _context;

        public UserRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername)
        {
            return await FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername);
        }

        public async Task<User?> GetByIdAsNoTrackingAsync(Guid userId)
        {
            return await QueryAsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<Guid>> GetExistingUserIdsAsync(List<Guid> userIds)
        {
            return await _context.Users
         .Where(u => userIds.Contains(u.UserId))
         .Select(u => u.UserId)
         .ToListAsync();
        }

        public async Task<List<User>> GetOnlineUsersAsync(List<Guid> userIds)
        {
            if (!userIds.Any()) return new List<User>();

            return await QueryAsNoTracking()
                .Where(u => userIds.Contains(u.UserId) && u.IsActive.GetValueOrDefault())
                .ToListAsync();
        }

        public async Task<User?> GetWithRolesAsync(Guid userId)
        {
            return await QueryAsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> IsEmailExistsAsync(string email, Guid? excludeUserId = null) =>
            await AnyAsync(u => u.Email == email
            && u.IsEmailVerified == true
            && (excludeUserId == null || u.UserId != excludeUserId));


        public async Task<bool> IsUsernameExistsAsync(string username, Guid? excludeUserId = null) =>
            await AnyAsync(u => u.Username == username
            && u.IsEmailVerified == true
            && (excludeUserId == null || u.UserId != excludeUserId));


        public async Task<List<User>> SearchUsersAsync(string searchTerm, Guid excludedUserId, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            var term = searchTerm.Trim().ToLower();

            return await QueryAsNoTracking()
                .Where(u => u.UserId != excludedUserId
                    && (u.IsActive ?? false)
                    && (u.Username.ToLower().Contains(term)
                    || (u.Fullname != null
                    && u.Fullname.ToLower().Contains(term)))
                ).OrderBy(u => u.Fullname ?? u.Username)
                .Take(Math.Clamp(limit, 1, 50))
                .ToListAsync();
        }
    }
}