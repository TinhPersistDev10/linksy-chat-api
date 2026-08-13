using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using linksy_backend_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace linksy_backend_api.Controllers
{
    // [Authorize]
    [EnableRateLimiting("public")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;

        public DebugController(ILogger<DebugController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint - không cần auth
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            _logger.LogInformation("Ping called - no auth required");
            return Ok(new
            {
                message = "pong",
                timestamp = DateTime.UtcNow,
                server = "Linksy API",
                isAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }

        /// <summary>
        /// Test endpoint - CẦN auth
        /// </summary>
        [Authorize]
        [HttpGet("secure-ping")]
        public IActionResult SecurePing()
        {
            _logger.LogInformation("Secure ping called - auth required");

            var userId = User.FindFirst("user_id")?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            _logger.LogInformation($"User: {userId}, {username}, {email}");

            return Ok(new
            {
                message = "secure pong - you are authenticated!",
                userId = userId,
                username = username,
                email = email,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Decode token để xem claims
        /// </summary>
        [HttpPost("decode-token")]
        public IActionResult DecodeToken([FromBody] TokenRequest request)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(request.Token))
                {
                    return BadRequest(new { error = "Invalid token format" });
                }

                var token = handler.ReadJwtToken(request.Token);

                var claims = token.Claims.Select(c => new
                {
                    type = c.Type,
                    value = c.Value,
                    shortType = c.Type.Split('/').Last() // Lấy phần cuối của claim type
                }).ToList();

                var result = new
                {
                    issuer = token.Issuer,
                    audiences = token.Audiences.ToList(),
                    validFrom = token.ValidFrom,
                    validTo = token.ValidTo,
                    isExpired = token.ValidTo < DateTime.UtcNow,
                    timeUntilExpiry = token.ValidTo - DateTime.UtcNow,
                    claims = claims,
                    claimsCount = claims.Count
                };

                _logger.LogInformation($"Token decoded: Issuer={token.Issuer}, Expired={result.isExpired}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding token");
                return BadRequest(new { error = ex.Message, details = ex.ToString() });
            }
        }

        /// <summary>
        /// Xem claims trong request hiện tại
        /// </summary>
        [Authorize]
        [HttpGet("my-claims")]
        public IActionResult GetMyClaims()
        {
            _logger.LogInformation("Getting my claims");

            var claims = User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value,
                shortType = c.Type.Contains('/') ? c.Type.Split('/').Last() : c.Type
            }).ToList();

            var result = new
            {
                isAuthenticated = User.Identity?.IsAuthenticated,
                authenticationType = User.Identity?.AuthenticationType,
                name = User.Identity?.Name,
                claimsCount = claims.Count,
                claims = claims,
                // Specific claims
                userId = User.FindFirst("user_id")?.Value,
                username = User.FindFirst(ClaimTypes.Name)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList()
            };

            _logger.LogInformation($"User {result.userId} has {claims.Count} claims");

            return Ok(result);
        }

        /// <summary>
        /// Test authorization với roles
        /// </summary>
        [Authorize(Roles = "admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(new { message = "You are an admin!" });
        }

        /// <summary>
        /// Kiểm tra token có valid không
        /// </summary>
        [HttpPost("validate-token")]
        public IActionResult ValidateToken([FromBody] TokenRequest request)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(request.Token))
                {
                    return Ok(new { valid = false, error = "Cannot read token" });
                }

                var token = handler.ReadJwtToken(request.Token);

                var isExpired = token.ValidTo < DateTime.UtcNow;
                var hasUserId = token.Claims.Any(c => c.Type == "user_id");

                return Ok(new
                {
                    valid = !isExpired,
                    isExpired = isExpired,
                    hasUserId = hasUserId,
                    issuer = token.Issuer,
                    audience = token.Audiences.FirstOrDefault(),
                    expiresAt = token.ValidTo,
                    timeRemaining = token.ValidTo - DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Ok(new { valid = false, error = ex.Message });
            }
        }

        public class TokenRequest
        {
            public string Token { get; set; }= string.Empty;
        }
    }
}