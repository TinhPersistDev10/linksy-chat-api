using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly LinksyDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;
        public UsersController(LinksyDbContext context, IUserService userService, ILogger<UsersController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                var result = await _userService.GetCurrentUserAsync(Guid.Parse(userId));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}