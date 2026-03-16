using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Lấy UserId từ JWT claim. Throws 401 nếu claim không tồn tại.
        /// </summary>
        protected Guid CurrentUserId
        {
            get
            {
                var value = User.FindFirstValue("user_id");
                if (value is null || !Guid.TryParse(value, out var userId))
                    throw new UnauthorizedAccessException("Missing or invalid user_id claim");
                return userId;
            }
        }

        /// <summary>
        /// Lấy UserId an toàn — trả về null thay vì throw.
        /// Dùng cho các endpoint có thể hoạt động cả khi anonymous.
        /// </summary>
        protected Guid? CurrentUserIdOrNull
        {
            get
            {
                var value = User.FindFirstValue("user_id");
                return Guid.TryParse(value, out var userId) ? userId : null;
            }
        }

    }
}