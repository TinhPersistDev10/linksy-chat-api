using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Auth;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Auth;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly ICacheService _cache;
        private readonly IUserModerationService _moderationService;
        private readonly ILogger<AuthService> _logger;
        public AuthService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IJwtService jwtService,
            ICacheService cache,
            IUserModerationService moderationService,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _jwtService = jwtService;
            _cache = cache;
            _moderationService = moderationService;
            _logger = logger;
        }   

        public async Task<ApiResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new Exception("Không tìm thấy email");

            // Đánh dấu các OTP cũ là expired
            var oldOtps = await _unitOfWork.EmailOtps.Query()
                .Where(o => o.Email == request.Email
                && o.Purpose == "password_reset"
                && o.IsUsed == false
                && o.IsExpired == false
                && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.IsExpired = true;
            }

            // Tạo OTP
            var otp = GenerateOtp();
            var emailOtp = new EmailOtp
            {
                UserId = user.UserId,
                Email = user.Email,
                OtpCode = otp,
                Purpose = "password_reset",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailOtps.AddAsync(emailOtp);
            await _unitOfWork.SaveChangesAsync();

            // Gửi email
            await _emailService.SendOtpEmailAsync(user.Email, user.Username, otp, "password_reset");

            return new ApiResponseDto
            {
                Success = true,
                Message = "OTP đã được gửi đến email của bạn"
            };
        }
        private async Task<UserInfoDto> MapToUserInfoAsync(User user)
        {
            var roles = await _unitOfWork.UserRoles.Query()
                .Where(ur => ur.UserId == user.UserId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();

            return new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                Fullname = user.Fullname ?? string.Empty,
                Avatar = user.Avatar ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive ?? false,
                IsEmailVerified = user.IsEmailVerified ?? false,
                CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                LastLoginAt = user.LastLoginAt,
                Roles = roles,
                Moderation = _moderationService.ToStatusDto(user)
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
                throw new Exception("Email/Username và mật khẩu không được để trống");

            // Tìm user theo email hoặc username
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

            var passwordHash = user?.PasswordHash ?? BCrypt.Net.BCrypt.HashPassword("dummy_timing_protection");
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, passwordHash);

            if (user == null)
                throw new Exception("Email/Username hoặc mật khẩu không đúng");

            // Kiểm tra account locked
            if (user.AccountLockedUntil.HasValue && user.AccountLockedUntil > DateTime.UtcNow)
            {
                var remainingTime = (int)Math.Ceiling((user.AccountLockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                throw new Exception($"Tài khoản đã bị khóa. Vui lòng thử lại sau {remainingTime} phút.");
            }

            // Verify password
            if (!isPasswordValid)
            {
                // Tăng failed login attempts
                user.FailedLoginAttempts++;

                // Khóa tài khoản nếu quá 5 lần
                if (user.FailedLoginAttempts >= 5)
                {
                    user.AccountLockedUntil = DateTime.UtcNow.AddMinutes(30);
                    await _unitOfWork.SaveChangesAsync();
                    throw new Exception("Tài khoản đã bị khóa do nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau 30 phút.");
                }

                await _unitOfWork.SaveChangesAsync();
                throw new Exception("Email/Username hoặc mật khẩu không đúng");
            }

            // Kiểm tra email verified
            if (user.IsEmailVerified != true)
            {
                var exception = new Exception("Vui lòng xác thực email trước khi đăng nhập");
                exception.Data["Email"] = user.Email;
                throw exception;
            }

            await _moderationService.RefreshExpiredModerationAsync(user, save: true);

            var moderationLevel = _moderationService.GetEffectiveLevel(user);
            if (ModerationLevels.BlocksLogin(moderationLevel))
            {
                if (moderationLevel == ModerationLevels.PermanentLock)
                {
                    throw new Exception(
                        "Tài khoản đã bị khóa vĩnh viễn do vi phạm tiêu chuẩn cộng đồng." +
                        (string.IsNullOrWhiteSpace(user.ModerationReason)
                            ? ""
                            : $" Lý do: {user.ModerationReason}"));
                }

                var until = user.ModerationExpiresAt?.ToString("dd/MM/yyyy HH:mm") ?? "không xác định";
                throw new Exception(
                    $"Tài khoản đang bị khóa tạm thời đến {until} UTC." +
                    (string.IsNullOrWhiteSpace(user.ModerationReason)
                        ? ""
                        : $" Lý do: {user.ModerationReason}"));
            }

            // Kiểm tra active (deactivate thủ công / permanent lock)
            if (user.IsActive != true)
                throw new Exception("Tài khoản đã bị vô hiệu hóa");

            // Reset failed attempts
            user.FailedLoginAttempts = 0;
            user.AccountLockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _cache.RemoveAsync(CacheKeys.UserProfile(user.UserId));
            await _cache.RemoveAsync(CacheKeys.UserRoles(user.UserId));

            //Clean up token cu
            await _unitOfWork.TokenRepository.CleanupExpiredAsync(user.UserId);
            // Tạo tokens
            var (accessToken, refreshToken, expiresAt, refreshExpiresAt) =
                await _jwtService.GenerateTokensAsync(user.UserId);

            // Lưu token
            var accessTokenEntity = new AccessToken
            {
                UserId = user.UserId,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.AccessTokens.AddAsync(accessTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                User = await MapToUserInfoAsync(user)
            };
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public async Task<ApiResponseDto> LogoutAsync(string token)
        {
            var accessToken = await _unitOfWork.TokenRepository.GetActiveByTokenAsync(token);

            if (accessToken == null)
                throw new Exception("Token không hợp lệ");

            accessToken.IsRevoked = true;
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đăng xuất thành công"
            };
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var tokenEntity = await _unitOfWork.TokenRepository.GetActiveByRefreshTokenAsync(request.RefreshToken);

            if (tokenEntity == null)
                throw new Exception("Refresh token không hợp lệ hoặc đã hết hạn");

            // Revoke old token
            tokenEntity.IsRevoked = true;

            // Tạo tokens mới
            var (accessToken, refreshToken, expiresAt, refreshExpiresAt) =
                await _jwtService.GenerateTokensAsync(tokenEntity.UserId);

            // Lưu token mới
            var newTokenEntity = new AccessToken
            {
                UserId = tokenEntity.UserId,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.AccessTokens.AddAsync(newTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new RefreshTokenResponse
            {
                Success = true,
                Message = "Token đã được làm mới",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password)
                )
                throw new Exception("Email và Username không được để trống");

            EnsurePasswordPolicy(request.Password);
            ProfileValidationHelper.EnsureDateOfBirth(request.DateOfBirth);
            if (!string.IsNullOrWhiteSpace(request.Username))
                ProfileValidationHelper.EnsureUsername(request.Username);

            // Kiểm tra email đã tồn tại
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
                u.Email == request.Email || u.Username == request.Username);

            if (existingUser != null)
            {

                if (existingUser.Email == request.Email && existingUser.IsEmailVerified != true)
                {
                    // Kiểm tra username mới có trùng user KHÁC không
                    var usernameTaken = await _unitOfWork.Users.AnyAsync(u =>
                        u.Username == request.Username &&
                        u.UserId != existingUser.UserId &&
                        u.IsEmailVerified == true);

                    if (usernameTaken)
                        throw new Exception("Username đã được sử dụng");

                    var oldOtps = await _unitOfWork.EmailOtps.Query()
                                            .Where(o => o.Email == request.Email
                                                && o.Purpose == "email_verification"
                                                && o.IsUsed == false)
                                            .ToListAsync();
                    foreach (var oldOtp in oldOtps)
                    {
                        oldOtp.IsExpired = true;
                    }
                    existingUser.Username = request.Username;
                    existingUser.PasswordHash = HashPassword(request.Password);
                    existingUser.Fullname = request.Fullname;
                    existingUser.DateOfBirth = request.DateOfBirth;
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    var newOtp = GenerateOtp();
                    var newEmailOtp = new EmailOtp
                    {
                        UserId = existingUser.UserId,
                        Email = existingUser.Email,
                        OtpCode = newOtp,
                        Purpose = "email_verification",
                        ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.EmailOtps.AddAsync(newEmailOtp);
                    await _unitOfWork.SaveChangesAsync();
                    await _emailService.SendOtpEmailAsync(existingUser.Email, existingUser.Username, newOtp, "email_verification");
                    return new RegisterResponse
                    {
                        UserId = existingUser.UserId,
                        Email = existingUser.Email,
                        Message = "Email đã tồn tại nhưng chưa được xác thực. Một OTP mới đã được gửi đến email của bạn.",
                        Success = true
                    };
                }
                // Email đã tồn tại VÀ đã verify → báo lỗi bình thường
                if (existingUser.Email == request.Email && existingUser.IsEmailVerified == true)
                    throw new Exception("Email đã được sử dụng");
                if (existingUser.Username == request.Username && existingUser.IsEmailVerified == true)
                    throw new Exception("Username đã được sử dụng");
            }

            // Tạo user mới
            var newUserId = Guid.NewGuid();
            var user = new User
            {
                UserId = newUserId,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Fullname = request.Fullname,
                DateOfBirth = request.DateOfBirth,
                // ⭐ Set avatar mặc định ngay khi tạo user
                Avatar = DefaultAvatarHelper.GetDefaultUserAvatar(newUserId, username: request.Username, fullname: request.Fullname),
                IsActive = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);

            // Gán role mặc định (user)
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = 2, // role "user"
                AssignedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserRoles.AddAsync(userRole);

            // Tạo OTP
            var otp = GenerateOtp();
            var emailOtp = new EmailOtp
            {
                UserId = user.UserId,
                Email = user.Email,
                OtpCode = otp,
                Purpose = "email_verification",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailOtps.AddAsync(emailOtp);
            await _unitOfWork.SaveChangesAsync();

            // Gửi email OTP
            await _emailService.SendOtpEmailAsync(user.Email, user.Username, otp, "email_verification");

            return new RegisterResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.",
                Success = true
            };
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty");
            var hashedBytes = BCrypt.Net.BCrypt.HashPassword(password);
            return hashedBytes;
        }

        private static void EnsurePasswordPolicy(string password)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                password.Length < 8 ||
                !password.Any(char.IsLetter) ||
                !password.Any(char.IsDigit))
            {
                throw new Exception("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ và số");
            }
        }

        private string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var random = BitConverter.ToUInt32(bytes, 0);
            return (random % 900000 + 100000).ToString(); // Tạo số từ 100000-999999
        }

        public async Task<ApiResponseDto> ResendOtpAsync(ResendOtpRequestDto request)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new Exception("Không tìm thấy email");

            // Đánh dấu các OTP cũ là expired
            var oldOtps = await _unitOfWork.EmailOtps.Query()
                .Where(o => o.Email == request.Email && o.Purpose == request.Purpose && o.IsUsed == false)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.IsExpired = true;
            }

            // Tạo OTP mới
            var otp = GenerateOtp();
            var emailOtp = new EmailOtp
            {
                UserId = user.UserId,
                Email = user.Email,
                OtpCode = otp,
                Purpose = request.Purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailOtps.AddAsync(emailOtp);
            await _unitOfWork.SaveChangesAsync();

            // Gửi email
            await _emailService.SendOtpEmailAsync(user.Email, user.Username, otp, request.Purpose);

            return new ApiResponseDto
            {
                Success = true,
                Message = "OTP mới đã được gửi đến email của bạn"
            };
        }

        public async Task<ApiResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            // Tìm OTP
            var otp = await _unitOfWork.EmailOtps.Query()
                .Where(o => o.Email == request.Email
                    && o.OtpCode == request.OtpCode
                    && o.Purpose == "password_reset"
                    && o.IsUsed == false
                    && o.IsExpired == false
                    && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                throw new Exception("OTP không hợp lệ hoặc đã hết hạn");

            if (otp.Attempts >= otp.MaxAttempts)
                throw new Exception("Đã vượt quá số lần thử");

            EnsurePasswordPolicy(request.NewPassword);

            otp.Attempts++;

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.UserId == otp.UserId);
            if (user == null)
                throw new Exception("Không tìm thấy user");

            // Cập nhật password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.AccountLockedUntil = null;

            otp.IsUsed = true;
            otp.VerifiedAt = DateTime.UtcNow;

            // Revoke tất cả tokens cũ
            var oldTokens = await _unitOfWork.AccessTokens.Query()
                .Where(t => t.UserId == user.UserId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
            }

            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đặt lại mật khẩu thành công"
            };
        }

        public async Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var otp = await _unitOfWork.EmailOtps.Query().Where(o => o.Email == request.Email
                    && o.OtpCode == request.OtpCode
                    && o.Purpose == "email_verification"
                    && o.IsUsed == false
                    && o.IsExpired == false
                    && o.ExpiresAt > DateTime.UtcNow
                )
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
                if (otp == null)
                    throw new Exception("OTP không hợp lệ hoặc đã hết hạn");

                if (otp.Attempts >= otp.MaxAttempts)
                    throw new Exception("Đã vượt quá số lần thử. Vui lòng yêu cầu OTP mới.");

                otp.Attempts++;
                otp.IsUsed = true;
                otp.VerifiedAt = DateTime.UtcNow;

                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.UserId == otp.UserId);
                if (user == null)
                    throw new Exception("Không tìm thấy user");

                user.IsEmailVerified = true;
                user.EmailVerifiedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                var (accessToken, refreshToken, expiresAt, refreshExpiresAt) =
                await _jwtService.GenerateTokensAsync(user.UserId);

                // Lưu access token
                var accessTokenEntity = new AccessToken
                {
                    UserId = user.UserId,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    RefreshTokenExpiresAt = refreshExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AccessTokens.AddAsync(accessTokenEntity);
                await _unitOfWork.CommitTransactionAsync();
                return new VerifyEmailResponse
                {
                    Success = true,
                    Message = "Xác thực email thành công!",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    RefreshTokenExpiresAt = refreshExpiresAt,
                    User = await MapToUserInfoAsync(user)
                };
            }
            catch (System.Exception ex)
            {

                await _unitOfWork.RollbackTransactionAsync();
                _logger?.LogError(ex, "Error verifying email");
                throw;
            }

            // Tạo token

        }
        public async Task<ApiResponseDto> ChangePasswordAsync(ChangePasswordRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //tìm người dùng
                var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);

                if (user == null)
                    throw new Exception("Không tìm thấy người dùng");

                //Kiểm tra mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    throw new Exception("Mật khẩu hiện tại không đúng");

                if (request.NewPassword != request.ConfirmPassword)
                    throw new Exception("Mật khẩu mới và xác nhận mật khẩu không khớp");

                EnsurePasswordPolicy(request.NewPassword);

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                var activeTokens = await _unitOfWork.AccessTokens.Query()
                    .Where(t => t.UserId == user.UserId && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                }

                _logger.LogInformation("User {UserId} changed password successfully", request.UserId);
                await _unitOfWork.CommitTransactionAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Đổi mật khẩu thành công"
                };
            }
            catch (System.Exception ex)
            {

                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
                throw;
            }
        }
    }
}
