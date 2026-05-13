using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Auth;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Auth;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Services
{
    public class AuthService : IAuthService
    {
        private readonly LinksyDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(LinksyDbContext context, IEmailService emailService, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _emailService = emailService;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<ApiResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new Exception("Không tìm thấy email");

            // Đánh dấu các OTP cũ là expired
            var oldOtps = await _context.EmailOtps
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

            await _context.EmailOtps.AddAsync(emailOtp);
            await _context.SaveChangesAsync();

            // Gửi email
            await _emailService.SendOtpEmailAsync(user.Email, user.Username, otp, "password_reset");

            return new ApiResponseDto
            {
                Success = true,
                Message = "OTP đã được gửi đến email của bạn"
            };
        }
        private UserInfoDto MapToUserInfo(User user)
        {
            return new UserInfoDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                Fullname = user.Fullname ?? string.Empty,
                Avatar = user.Avatar ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                IsEmailVerified = user.IsEmailVerified ?? false,
                CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrUsername) || string.IsNullOrWhiteSpace(request.Password))
                throw new Exception("Email/Username và mật khẩu không được để trống");

            // Tìm user theo email hoặc username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername
                    || u.Username == request.EmailOrUsername);

            if (user == null)
                throw new Exception("Email/Username hoặc mật khẩu không đúng");

            // Kiểm tra account locked
            if (user.AccountLockedUntil.HasValue && user.AccountLockedUntil > DateTime.UtcNow)
            {
                var remainingTime = (user.AccountLockedUntil.Value - DateTime.UtcNow).Minutes;
                throw new Exception($"Tài khoản đã bị khóa. Vui lòng thử lại sau {remainingTime} phút.");
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                // Tăng failed login attempts
                user.FailedLoginAttempts++;

                // Khóa tài khoản nếu quá 5 lần
                if (user.FailedLoginAttempts >= 5)
                {
                    user.AccountLockedUntil = DateTime.UtcNow.AddMinutes(30);
                    await _context.SaveChangesAsync();
                    throw new Exception("Tài khoản đã bị khóa do nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau 30 phút.");
                }

                await _context.SaveChangesAsync();
                throw new Exception("Email/Username hoặc mật khẩu không đúng");
            }

            // Kiểm tra email verified
            if (user.IsEmailVerified == false || user.IsEmailVerified == null)
                throw new Exception("Vui lòng xác thực email trước khi đăng nhập");

            // Kiểm tra active
            if (user.IsActive == false || user.IsActive == null)
                throw new Exception("Tài khoản đã bị vô hiệu hóa");

            // Reset failed attempts
            user.FailedLoginAttempts = 0;
            user.AccountLockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

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
            await _context.AccessTokens.AddAsync(accessTokenEntity);
            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                User = MapToUserInfo(user)
            };
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public async Task<ApiResponseDto> LogoutAsync(string token)
        {
            var accessToken = await _context.AccessTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked);

            if (accessToken == null)
                throw new Exception("Token không hợp lệ");

            accessToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đăng xuất thành công"
            };
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var tokenEntity = await _context.AccessTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == request.RefreshToken
                    && !t.IsRevoked
                    && t.RefreshTokenExpiresAt > DateTime.UtcNow);

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
            await _context.AccessTokens.AddAsync(newTokenEntity);
            await _context.SaveChangesAsync();

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
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Username))
                throw new Exception("Email và Username không được để trống");
            if (request.Password.Length < 6)
                throw new Exception("Mật khẩu phải có ít nhất 6 ký tự");

            // Kiểm tra email đã tồn tại
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);

            if (existingUser != null)
            {

                if (existingUser.Email == request.Email && existingUser.IsEmailVerified != true)
                {
                    // Kiểm tra username mới có trùng user KHÁC không
                    var usernameTaken = await _context.Users
                        .AnyAsync(u => u.Username == request.Username
                            && u.UserId != existingUser.UserId
                            && u.IsEmailVerified == true);

                    if (usernameTaken)
                        throw new Exception("Username đã được sử dụng");

                    var oldOtps = await _context.EmailOtps
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
                    existingUser.DateOfBirth = request.DateOfBirth.HasValue
                        ? DateOnly.FromDateTime(request.DateOfBirth.Value) : null;
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
                    await _context.EmailOtps.AddAsync(newEmailOtp);
                    await _context.SaveChangesAsync();
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
                DateOfBirth = request.DateOfBirth.HasValue
                                ? DateOnly.FromDateTime(request.DateOfBirth.Value) : null,
                // ⭐ Set avatar mặc định ngay khi tạo user
                Avatar = DefaultAvatarHelper.GetDefaultUserAvatar(newUserId, username: request.Username, fullname: request.Fullname),
                IsActive = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);

            // Gán role mặc định (user)
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = 2, // role "user"
                AssignedAt = DateTime.UtcNow
            };
            await _context.UserRoles.AddAsync(userRole);

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

            await _context.EmailOtps.AddAsync(emailOtp);
            await _context.SaveChangesAsync();

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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new Exception("Không tìm thấy email");

            // Đánh dấu các OTP cũ là expired
            var oldOtps = await _context.EmailOtps
                .Where(o => o.Email == request.Email && o.Purpose == request.Purpose && !o.IsUsed !=true)
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

            await _context.EmailOtps.AddAsync(emailOtp);
            await _context.SaveChangesAsync();

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
            var otp = await _context.EmailOtps
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

            otp.Attempts++;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == otp.UserId);
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
            var oldTokens = await _context.AccessTokens
                .Where(t => t.UserId == user.UserId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đặt lại mật khẩu thành công"
            };
        }

        public async Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var otp = await _context.EmailOtps.Where(o => o.Email == request.Email
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

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == otp.UserId);
                if (user == null)
                    throw new Exception("Không tìm thấy user");

                user.IsEmailVerified = true;
                user.EmailVerifiedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
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
                await _context.AccessTokens.AddAsync(accessTokenEntity);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new VerifyEmailResponse
                {
                    Success = true,
                    Message = "Xác thực email thành công!",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    RefreshTokenExpiresAt = refreshExpiresAt,
                    User = MapToUserInfo(user)
                };
            }
            catch (System.Exception ex)
            {

                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error verifying email");
                throw;
            }

            // Tạo token

        }
        public async Task<ApiResponseDto> ChangePasswordAsync(ChangePasswordRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //tìm người dùng
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);

                if (user == null)
                    throw new Exception("Không tìm thấy người dùng");

                //Kiểm tra mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    throw new Exception("Mật khẩu hiện tại không đúng");

                if (request.NewPassword != request.ConfirmPassword)
                    throw new Exception("Mật khẩu mới và xác nhận mật khẩu không khớp");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                var activeTokens = await _context.AccessTokens
                    .Where(t => t.UserId == user.UserId && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                }

                _logger.LogInformation("User {UserId} changed password successfully", request.UserId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Đổi mật khẩu thành công"
                };
            }
            catch (System.Exception ex)
            {

                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
                throw;
            }
        }
    }
}