using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    // ── UserSettings ──────────────────────────────────────────────────────────
    public class UserSettingsRepository : Repository<UserSettings>, IUserSettingsRepository
    {
        private readonly LinksyDbContext _context;

        public UserSettingsRepository(LinksyDbContext context) : base(context) => _context = context;

        public async Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);

        public async Task<UserSettings> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
        {
            var settings = await GetByUserIdAsync(userId, ct);
            if (settings is not null) return settings;

            settings = new UserSettings
            {
                SettingId = Guid.NewGuid(),
                UserId = userId,
                Language = "vi",
                Timezone = "Asia/Ho_Chi_Minh",
                Theme = "system",
                CreatedAt = DateTime.UtcNow
            };
            await _context.UserSettings.AddAsync(settings, ct);
            return settings;
        }
    }

    // ── NotificationSettings ──────────────────────────────────────────────────
    public class NotificationSettingsRepository : Repository<NotificationSettings>, INotificationSettingsRepository
    {
        private readonly LinksyDbContext _context;

        public NotificationSettingsRepository(LinksyDbContext context) : base(context) => _context = context;

        public async Task<NotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _context.NotificationSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);

        public async Task<NotificationSettings> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
        {
            var settings = await GetByUserIdAsync(userId, ct);
            if (settings is not null) return settings;

            settings = new NotificationSettings
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationsEnabled = true,
                NotificationSoundEnabled = true,
                MessagePreviewEnabled = true,
                EmailNotifications = false
            };
            await _context.NotificationSettings.AddAsync(settings, ct);
            return settings;
        }
    }

    // ── PrivacySettings ───────────────────────────────────────────────────────
    public class PrivacySettingsRepository : Repository<PrivacySettings>, IPrivacySettingsRepository
    {
        private readonly LinksyDbContext _context;

        public PrivacySettingsRepository(LinksyDbContext context) : base(context) => _context = context;

        public async Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _context.PrivacySettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);

        public async Task<PrivacySettings> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
        {
            var settings = await GetByUserIdAsync(userId, ct);
            if (settings is not null) return settings;

            settings = new PrivacySettings
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReadReceiptsEnabled = true,
                TypingIndicatorsEnabled = true,
                LastSeenEnabled = true,
                ProfilePhotoVisibility = "everyone",
                StatusVisibility = "everyone",
                WhoCanAddToGroups = "everyone"
            };
            await _context.PrivacySettings.AddAsync(settings, ct);
            return settings;
        }
    }

    // ── UserStatus ────────────────────────────────────────────────────────────
    public class UserStatusRepository : Repository<UserStatus>, IUserStatusRepository
    {
        private readonly LinksyDbContext _context;

        public UserStatusRepository(LinksyDbContext context) : base(context) => _context = context;

        public async Task<UserStatus?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _context.UserStatuses.FirstOrDefaultAsync(s => s.UserId == userId, ct);

        public async Task<UserStatus> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
        {
            var status = await GetByUserIdAsync(userId, ct);
            if (status is not null) return status;

            status = new UserStatus
            {
                StatusId = Guid.NewGuid(),
                UserId = userId,
                StatusType = "offline",
                UpdatedAt = DateTime.UtcNow
            };
            await _context.UserStatuses.AddAsync(status, ct);
            return status;
        }

        public async Task<List<UserStatus>> GetOnlineStatusesAsync(List<Guid> userIds, CancellationToken ct = default)
            => await _context.UserStatuses
                .Where(s => userIds.Contains(s.UserId) && s.StatusType != "offline")
                .ToListAsync(ct);
    }
}