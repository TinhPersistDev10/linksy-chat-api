using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;

namespace linksy_backend_api.Infrastructure.Mappers
{
    public class UserMapper
    {
        public static UserInfoDto ToResponse(User user, IEnumerable<string>? roles = null)
        {
            return new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                Fullname = user.Fullname ?? string.Empty,
                Avatar = DefaultAvatarHelper.GetAvatarOrDefault(user.Avatar, user.UserId, username: user.Username, fullname: user.Fullname),
                Bio = user.Bio ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive ?? false,
                IsEmailVerified = user.IsEmailVerified ?? false,
                CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                LastLoginAt = user.LastLoginAt,
                Roles = roles?.ToList() ?? new List<string>()
            };
        }


        public static List<UserInfoDto> ToResponseList(IEnumerable<User> users)
            => users.Select(u => ToResponse(u)).ToList();
    }
}
