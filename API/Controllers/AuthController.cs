using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Auth;
using linksy_backend_api.DTOs;
using linksy_backend_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.Controllers
{

    // [Authorize]
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
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);

                return Ok(result);
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
        private CookieOptions GetCookieOptions(DateTimeOffset? expires = null)
        {
            var isDevelopment = _configuration.GetValue<bool>("IsDevelopment", true);
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
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
            var isDevelopment = _configuration.GetValue<bool>("IsDevelopment", true);

            var expiredOptions = new CookieOptions
            {
                HttpOnly = true,                    // ← BẮT BUỘC phải có
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UnixEpoch  // Set về 1970 → browser xóa ngay
            };

            Response.Cookies.Append("accessToken", "", expiredOptions);
            Response.Cookies.Append("refreshToken", "", expiredOptions);
        }
        // [Authorize]
        [HttpPost("logout")]
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
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
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

                var isDevelopment = _configuration.GetValue<bool>("IsDevelopment", true);

                // Update cookies với token mới
                var accessTokenOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = result.ExpiresAt,
                    Path = "/",
                    IsEssential = true
                };

                var refreshTokenOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = result.RefreshTokenExpiresAt,
                    Path = "/",
                    IsEssential = true
                };

                Response.Cookies.Append("accessToken", result.AccessToken, accessTokenOptions);
                Response.Cookies.Append("refreshToken", result.RefreshToken, refreshTokenOptions);

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
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");

                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Quên mật khẩu - Gửi OTP
        /// </summary>
        [HttpPost("forgot-password")]
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
