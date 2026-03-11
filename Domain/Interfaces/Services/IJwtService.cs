using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace linksy_backend_api.Services
{
    public interface IJwtService
    {
        Task<(string accessToken, string refreshToken, DateTime expiresAt, DateTime refreshExpiresAt)> GenerateTokensAsync(Guid userId);
        ClaimsPrincipal ValidateToken(string token);
    }
}