using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Auth;
using linksy_backend_api.DTOs;
using linksy_backend_api.Domain.Options;
using linksy_backend_api.Infrastructure.Exceptions;
using linksy_backend_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace linksy_backend_api.Controllers
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration)  // ← Thêm này
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;  // ← Và này
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// 
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitingOptions.AuthSensitivePolicy)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);

                return Ok(result);
            }
            catch (RegisterConflictException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    errors = ex.FieldErrors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xác thực OTP khi đăng ký
        /// </summary>
        [HttpPost("verify-email")]
        [EnableRateLimiting(RateLimitingOptions.AuthSensitivePolicy)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
        {
            try
            {
                var result = await _authService.VerifyEmailAsync(request);
                SetAuthCookies(result.AccessToken, result.RefreshToken,
                    result.ExpiresAt, result.RefreshTokenExpiresAt);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gửi lại OTP
        /// </summary>
        [HttpPost("resend-otp")]
        [EnableRateLimiting(RateLimitingOptions.AuthSensitivePolicy)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
        {
            try
            {
                var result = await _authService.ResendOtpAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitingOptions.AuthLoginPolicy)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                SetAuthCookies(result.AccessToken, result.RefreshToken,
                    result.ExpiresAt, result.RefreshTokenExpiresAt);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                if (ex.Data.Contains("Email"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        success = false,
                        message = ex.Message,
                        email = ex.Data["Email"]?.ToString()
                    });
                }

                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        private bool IsLocalHttpRequest()
        {
            if (Request.IsHttps) return false;

            var host = Request.Host.Host;
            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("::1", StringComparison.OrdinalIgnoreCase);
        }

        private CookieOptions GetCookieOptions(DateTimeOffset? expires = null)
        {
            var isLocalHttp = IsLocalHttpRequest();
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !isLocalHttp,
                SameSite = isLocalHttp ? SameSiteMode.Lax : SameSiteMode.None,
                Path = "/",
                IsEssential = true,
                Expires = expires
            };
        }

        private void SetAuthCookies(string accessToken, string refreshToken,
            DateTimeOffset accessExpires, DateTimeOffset refreshExpires)
        {
            Response.Cookies.Append("accessToken", accessToken, GetCookieOptions(accessExpires));
            Response.Cookies.Append("refreshToken", refreshToken, GetCookieOptions(refreshExpires));
        }

        private void ClearAuthCookies()
        {
            var cookieOptions = GetCookieOptions(DateTimeOffset.UnixEpoch);
            var expiredOptions = new CookieOptions
            {
                HttpOnly = true,                    // ← BẮT BUỘC phải có
                Secure = cookieOptions.Secure,
                SameSite = cookieOptions.SameSite,
                Path = "/",
                IsEssential = true,
                Expires = DateTimeOffset.UnixEpoch  // Set về 1970 → browser xóa ngay
            };

            Response.Cookies.Append("accessToken", "", expiredOptions);
            Response.Cookies.Append("refreshToken", "", expiredOptions);
        }
        // [Authorize]
        [HttpPost("logout")]
        [EnableRateLimiting(RateLimitingOptions.ApiPolicy)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = Request.Cookies["accessToken"];

                // Vẫn cố revoke token nếu có, nhưng không bắt buộc
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        await _authService.LogoutAsync(token);
                    }
                    catch
                    {
                        // Bỏ qua lỗi nếu token không tìm thấy hoặc đã revoked
                        // Vẫn tiếp tục xóa cookie
                    }
                }

                // Luôn xóa cookie dù token có hợp lệ hay không
                ClearAuthCookies();

                return Ok(new { success = true, message = "Đăng xuất thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");

                // Vẫn xóa cookie kể cả khi có lỗi
                ClearAuthCookies();

                return Ok(new { success = true, message = "Đăng xuất thành công" });
            }
        }

        /// <summary>
        /// Refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [EnableRateLimiting(RateLimitingOptions.ApiPolicy)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                // Lấy refresh token từ cookie
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Refresh token không tồn tại"
                    });
                }

                var refreshRequest = new RefreshTokenRequestDto
                {
                    RefreshToken = refreshToken
                };

                var result = await _authService.RefreshTokenAsync(refreshRequest);

                SetAuthCookies(result.AccessToken, result.RefreshToken,
                    result.ExpiresAt, result.RefreshTokenExpiresAt);

                return Ok(new
                {
                    success = true,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");

                // Nếu refresh token hết hạn, xóa cookies
                ClearAuthCookies();
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        /// Quên mật khẩu - Gửi OTP
        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitingOptions.AuthSensitivePolicy)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                var result = await _authService.ForgotPasswordAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu với OTP
        /// </summary>
        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitingOptions.AuthSensitivePolicy)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>

        [Authorize]
        [HttpPost("change-password")]
        [EnableRateLimiting(RateLimitingOptions.ApiPolicy)]
        public async Task<ActionResult<ApiResponseDto>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    return Unauthorized(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Không xác thực được người dùng"
                    });
                }

                request.UserId = userGuid;

                var result = await _authService.ChangePasswordAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}
