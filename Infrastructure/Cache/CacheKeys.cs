// Infrastructure/Cache/CacheKeys.cs
namespace linksy_backend_api.Infrastructure.Cache
{
    public static class CacheKeys
    {
        // ── User ────────────────────────────────────────────────────────────
        public static string UserProfile(Guid userId)      => $"user:profile:{userId}";
        public static string UserSettings(Guid userId)     => $"user:settings:{userId}";
        public static string UserPrivacy(Guid userId)      => $"user:privacy:{userId}";
        public static string UserNotifSettings(Guid userId)=> $"user:notif-settings:{userId}";
        public static string UserStatus(Guid userId)       => $"user:status:{userId}";
        public static string UserFriends(Guid userId)      => $"user:friends:{userId}";
        public static string UserRoles(Guid userId)        => $"user:roles:{userId}";

        // ── Chatroom ─────────────────────────────────────────────────────────
        public static string ChatroomList(Guid userId, bool includeArchived, string? type)     
        => $"chatroom:list:{userId}:archived:{includeArchived}:type:{type}";
        public static string ChatroomDetail(Guid userId, Guid chatroomId)       
        => $"chatroom:detail:{chatroomId}:user:{userId}";
        public static string ChatroomMembers(Guid id)      => $"chatroom:members:{id}";

        // ── Messages ─────────────────────────────────────────────────────────
        public static string Messages(Guid chatroomId, int page, int pageSize) 
        => $"msg:{chatroomId}:p{page}:s:{pageSize}";
        public static string MessageReactions(Guid messageId)    => $"msg:reactions:{messageId}";

        // ── Notifications ─────────────────────────────────────────────────────
        public static string NotifUnreadCount(Guid userId) => $"notif:unread:{userId}";
        public static string NotifList(Guid userId, int page) => $"notif:list:{userId}:p{page}";

        // ── Expiry presets ──────────────────────────────────────────────────
        public static readonly TimeSpan ShortTtl  = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan MediumTtl = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan LongTtl   = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan HourTtl   = TimeSpan.FromHours(1);
    }
}
