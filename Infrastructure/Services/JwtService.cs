using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace linksy_backend_api.Services
{
    public class JwtService : IJwtService
    {
        private readonly LinksyDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public JwtService(LinksyDbContext context, IConfiguration configuration, ILogger<JwtService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            _jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured in appsettings.json");
            _jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured in appsettings.json");

            _logger.LogInformation("JWT Service initialized with Issuer: {Issuer}, Audience: {Audience}",
                _jwtIssuer, _jwtAudience);
        }

        async Task<(string accessToken, string refreshToken, DateTime expiresAt, DateTime refreshExpiresAt)> IJwtService.GenerateTokensAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var claims = new List<Claim>
            {
                new Claim("user_id", user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));
            }

            // Tạo Access Token
            var jwtKey = _configuration.GetRequiredSection("Jwt:Key").Value;
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(1); // Access token hết hạn sau 1 giờ

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Tạo Refresh Token
            var refreshToken = GenerateRefreshToken();
            var refreshExpiresAt = DateTime.UtcNow.AddDays(7); // Refresh token hết hạn sau 7 ngày

            return (accessToken, refreshToken, expiresAt, refreshExpiresAt);

        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        ClaimsPrincipal IJwtService.ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(_jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException("Token validation failed", ex);
            }
        }
    }
}