using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Infrastructure.Helpers
{
    public static class DefaultAvatarHelper
    {
        public const string CloudiaryName = "linksyapi";
        public const string DefaultGroupAvatar = "https://res.cloudinary.com/linksyapi/image/upload/v1773207406/group_white_zbpe9t.png";

        /// Lấy avatar mặc định dựa trên userId (để random)
        public static string GenerateTextAvatar(string fullName, Guid userId)
        {
            var initial = "U";
            if (!string.IsNullOrEmpty(fullName))
            {
                var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    initial = $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                else initial = parts[0][0].ToString().ToUpper();
            }
            var colors = new[] { "4F46E5", "7C3AED", "DB2777", "DC2626", "D97706", "059669", "0891B2" };
            var colorIndex = Math.Abs(userId.GetHashCode()) % colors.Length;
            var bgColor = colors[colorIndex];
            return $"https://res.cloudinary.com/{CloudiaryName}/image/upload/" +
                    $"w_800,h_800,c_fill,b_rgb:{bgColor}/" +
                    $"l_text:Roboto_320_bold:{initial},co_white,g_center/" +
                    $"blank_a3thk2.png";
        }
        public static string GetDefaultUserAvatar(Guid userId, string? username = null, string? fullname = null)
        {
            var displayName = !string.IsNullOrEmpty(fullname) ? fullname : username;
            if (!string.IsNullOrEmpty(displayName))
                return GenerateTextAvatar(displayName, userId);

            return GenerateTextAvatar("U", userId);
        }

        /// <summary>
        /// Lấy avatar mặc định cho group
        /// </summary>
        public static string GetDefaultGroupAvatar()
        {
            return DefaultGroupAvatar;
        }

        /// <summary>
        /// Kiểm tra xem URL có phải avatar mặc định không
        /// </summary>
        public static bool IsDefaultAvatar(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
                return true;

            if (avatarUrl.Contains("l_text:") || avatarUrl.Contains("/defaults/"))
                return true;

            return avatarUrl == DefaultGroupAvatar;
        }

        /// <summary>
        /// Lấy avatar (return default nếu null/empty)
        /// </summary>
        public static string GetAvatarOrDefault(string? avatarUrl, Guid? userId = null, string? username = null, string? fullname = null)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                return userId.HasValue
                    ? GetDefaultUserAvatar(userId.Value, username, fullname)
                    : GenerateTextAvatar("U", Guid.Empty);
            }

            return avatarUrl;
        }
    }
}