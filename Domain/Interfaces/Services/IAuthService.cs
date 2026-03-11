using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Auth;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Auth;
using linksy_backend_api.DTOs.UserDTO;

namespace linksy_backend_api.Services
{
    public interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequestDto request);
        Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequestDto request);
        Task<ApiResponseDto> ResendOtpAsync(ResendOtpRequestDto request);
        Task<LoginResponse> LoginAsync(LoginRequestDto request);
        Task<ApiResponseDto> LogoutAsync(string token);
        Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<ApiResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<ApiResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<ApiResponseDto> ChangePasswordAsync(ChangePasswordRequest request);
    }
}