using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUnitOfWork unitOfWork, ILogger<AdminService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponseDto> AssignRoleAsync(AssignRoleDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
                if (user == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        Data = ""
                    };
                }

                var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
                if (role == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "Role not found",
                        Data = ""
                    };
                }

                var existingRole = await _unitOfWork.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.RoleId == dto.RoleId);

                if (existingRole != null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "Role already assigned to user",
                        Data = ""
                    };
                }

                var userRole = new UserRole
                {
                    UserId = dto.UserId,
                    RoleId = dto.RoleId,
                    AssignedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserRoles.AddAsync(userRole);
                await _unitOfWork.SaveChangesAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Role assigned successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to assign role",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> CreateUserAsync(CreateUserByAdminDto dto)
        {
            try
            {
                var existingEmail = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingEmail != null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "Email already exists",
                        Data = ""
                    };
                }

                var existingUsername = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
                if (existingUsername != null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "Username already exists",
                        Data = ""
                    };
                }

                await _unitOfWork.BeginTransactionAsync();

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Fullname = dto.Fullname,
                    Bio = dto.Bio,
                    DateOfBirth = dto.DateOfBirth,
                    IsActive = dto.IsActive,
                    IsEmailVerified = dto.IsEmailVerified,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);

                if (dto.RoleId.HasValue)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = dto.RoleId.Value,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserRoles.AddAsync(userRole);
                }

                await _unitOfWork.CommitTransactionAsync();

                var roles = dto.RoleId.HasValue
                    ? new List<string> { (await _unitOfWork.Roles.GetByIdAsync(dto.RoleId.Value))?.RoleName ?? "User" }
                    : new List<string>();

                var userDto = new AdminUserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Fullname = user.Fullname,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive ?? true,
                    IsEmailVerified = user.IsEmailVerified ?? false,
                    CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                    Roles = roles
                };

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "User created successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating user");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to create user",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        Data = ""
                    };
                }

                // Kiểm tra xem user có phải admin cuối cùng không
                var isAdmin = await _unitOfWork.UserRoles.Query()
                    .AnyAsync(ur => ur.UserId == userId && ur.Role.RoleName == "Admin");

                if (isAdmin)
                {
                    var adminCount = await _unitOfWork.UserRoles.Query()
                        .Where(ur => ur.Role.RoleName == "Admin")
                        .CountAsync();

                    if (adminCount <= 1)
                    {
                        return new ApiResponseDto
                        {
                            Success = false,
                            Message = "Cannot delete the last admin user",
                            Data = ""
                        };
                    }
                }

                await _unitOfWork.BeginTransactionAsync();

                // 1. Xóa Access Tokens
                var tokens = await _unitOfWork.AccessTokens.Query()
                    .Where(t => t.UserId == userId)
                    .ToListAsync();
                _unitOfWork.AccessTokens.RemoveRange(tokens);

                // 2. Xóa Email OTPs
                var otps = await _unitOfWork.EmailOtps.Query()
                    .Where(o => o.UserId == userId)
                    .ToListAsync();
                _unitOfWork.EmailOtps.RemoveRange(otps);

                // 3. Xóa Friend Requests
                var friendRequests = await _unitOfWork.FriendRequests.Query()
                    .Where(fr => fr.SenderId == userId || fr.ReceiverId == userId)
                    .ToListAsync();
                _unitOfWork.FriendRequests.RemoveRange(friendRequests);

                // 4. Xóa Friendships
                var friendships = await _unitOfWork.Friendships.Query()
                    .Where(f => f.User1Id == userId || f.User2Id == userId)
                    .ToListAsync();
                _unitOfWork.Friendships.RemoveRange(friendships);

                // 5. Xóa Block relationships
                var blocks = await _unitOfWork.BlockedUsers.Query()
                    .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
                    .ToListAsync();
                _unitOfWork.BlockedUsers.RemoveRange(blocks);

                // 6. Xóa Notifications
                var notifications = await _unitOfWork.Notifications.Query()
                    .Where(n => n.UserId == userId)
                    .ToListAsync();
                _unitOfWork.Notifications.RemoveRange(notifications);

                // 7. Xóa User Roles
                var userRoles = await _unitOfWork.UserRoles.Query()
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();
                _unitOfWork.UserRoles.RemoveRange(userRoles);

                // 8. Xóa Group Invitations
                var invitations = await _unitOfWork.GroupInvitations.Query()
                    .Where(gi => gi.InvitedUserId == userId || gi.InvitedBy == userId)
                    .ToListAsync();
                _unitOfWork.GroupInvitations.RemoveRange(invitations);

                // 9. Xử lý Role Members
                var memberships = await _unitOfWork.ChatroomMembers.Query()
                    .Where(rm => rm.UserId == userId)
                    .ToListAsync();
                _unitOfWork.ChatroomMembers.RemoveRange(memberships);

                // ===== QUAN TRỌNG: Xử lý Messages và Chatrooms =====

                // Lấy tất cả message IDs của user
                var userMessageIds = await _unitOfWork.Messages.Query()
                    .Where(m => m.SenderId == userId)
                    .Select(m => m.MessageId)
                    .ToListAsync();

                // 10. Nullify last_message_id của TẤT CẢ chatrooms có messages của user này
                if (userMessageIds.Any())
                {
                    var chatroomsWithUserMessages = await _unitOfWork.Chatrooms.Query()
                        .Where(c => c.LastMessageId.HasValue && userMessageIds.Contains(c.LastMessageId.Value))
                        .ToListAsync();

                    foreach (var chatroom in chatroomsWithUserMessages)
                    {
                        chatroom.LastMessageId = null;
                    }
                    _unitOfWork.Chatrooms.UpdateRange(chatroomsWithUserMessages);
                }

                // 11. Xử lý Chatrooms do user tạo
                var userCreatedChatrooms = await _unitOfWork.Chatrooms.Query()
                    .Where(c => c.CreatedBy == userId)
                    .ToListAsync();

                // Nullify last_message_id của chatrooms do user tạo (nếu chưa null)
                foreach (var chatroom in userCreatedChatrooms.Where(c => c.LastMessageId.HasValue))
                {
                    chatroom.LastMessageId = null;
                }
                _unitOfWork.Chatrooms.UpdateRange(userCreatedChatrooms);

                // Lưu tất cả thay đổi last_message_id = NULL
                await _unitOfWork.SaveChangesAsync();

                // 12. Bây giờ mới XÓA messages của user (an toàn rồi)
                var userMessages = await _unitOfWork.Messages.Query()
                    .Where(m => m.SenderId == userId)
                    .ToListAsync();
                _unitOfWork.Messages.RemoveRange(userMessages);

                // 13. Xử lý xóa hoặc chuyển ownership chatrooms
                foreach (var chatroom in userCreatedChatrooms)
                {
                    if (chatroom.RoomType == "direct")
                    {
                        // Xóa toàn bộ direct chat
                        var chatroomMessages = await _unitOfWork.Messages.Query()
                            .Where(m => m.ChatroomId == chatroom.ChatroomId)
                            .ToListAsync();
                        _unitOfWork.Messages.RemoveRange(chatroomMessages);

                        var chatroomMembers = await _unitOfWork.ChatroomMembers.Query()
                            .Where(rm => rm.ChatroomId == chatroom.ChatroomId)
                            .ToListAsync();
                        _unitOfWork.ChatroomMembers.RemoveRange(chatroomMembers);

                        _unitOfWork.Chatrooms.Remove(chatroom);
                    }
                    else
                    {
                        // Chuyển ownership cho admin còn lại trong group
                        var newOwner = await _unitOfWork.ChatroomMembers.Query()
                            .Where(rm => rm.ChatroomId == chatroom.ChatroomId
                                && rm.UserId != userId
                                && rm.MemberRole == "admin"
                                && rm.LeftAt == null)
                            .FirstOrDefaultAsync();

                        if (newOwner != null)
                        {
                            chatroom.CreatedBy = newOwner.UserId;
                            _unitOfWork.Chatrooms.Update(chatroom);
                        }
                        else
                        {
                            // Không có admin khác, xóa toàn bộ chatroom
                            var chatroomMessages = await _unitOfWork.Messages.Query()
                                .Where(m => m.ChatroomId == chatroom.ChatroomId)
                                .ToListAsync();
                            _unitOfWork.Messages.RemoveRange(chatroomMessages);

                            var chatroomMembers = await _unitOfWork.ChatroomMembers.Query()
                                .Where(rm => rm.ChatroomId == chatroom.ChatroomId)
                                .ToListAsync();
                            _unitOfWork.ChatroomMembers.RemoveRange(chatroomMembers);

                            _unitOfWork.Chatrooms.Remove(chatroom);
                        }
                    }
                }

                // 14. Cuối cùng, xóa User
                _unitOfWork.Users.Remove(user);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {UserId} has been permanently deleted", userId);

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "User permanently deleted",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return new ApiResponseDto
                {
                    Success = false,
                    Message = $"Failed to delete user: {ex.Message}",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> GetAllRolesAsync()
        {
            try
            {
                var roles = await _unitOfWork.Roles.Query()
                    .Select(r => new RoleDto
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        Description = r.Description
                    })
                    .ToListAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Roles retrieved successfully",
                    Data = roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to retrieve roles",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> GetAllUsersAsync(int page, int pageSize, string? searchTerm)
        {
            try
            {
                var query = _unitOfWork.Users.QueryAsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(u =>
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        (u.Fullname != null && u.Fullname.ToLower().Contains(searchTerm))
                    );
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AdminUserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        Fullname = u.Fullname,
                        Avatar = u.Avatar,
                        IsActive = u.IsActive ?? true,
                        IsEmailVerified = u.IsEmailVerified ?? false,
                        CreatedAt = u.CreatedAt ?? DateTime.UtcNow,
                        LastLoginAt = u.LastLoginAt,
                        Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        CurrentPage = page,
                        TotalCount = totalCount
                    })
                    .ToListAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Users retrieved successfully",
                    Data = users
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to retrieve users",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> GetRecentActivitiesAsync(int limit)
        {
            try
            {
                var recentUsers = await _unitOfWork.Users.Query()
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(limit)
                    .Select(u => new RecentActivityDto
                    {
                        ActivityType = "User Registration",
                        Description = $"New user '{u.Username}' registered",
                        Timestamp = u.CreatedAt ?? DateTime.UtcNow,
                        UserId = u.UserId,
                        Username = u.Username
                    })
                    .ToListAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Recent activities retrieved successfully",
                    Data = recentUsers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to retrieve recent activities",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> GetStatisticsAsync()
        {
            try
            {
                var totalUsers = await _unitOfWork.Users.CountAsync(null);
                var activeUsers = await _unitOfWork.Users.CountAsync(u => u.IsActive == true);
                var totalMessages = await _unitOfWork.Messages.CountAsync(null);
                var totalChatrooms = await _unitOfWork.Chatrooms.CountAsync(null);

                var stats = new AdminStatisticsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = totalUsers - activeUsers,
                    TotalMessages = totalMessages,
                    TotalChatrooms = totalChatrooms,
                    NewUsersThisMonth = await _unitOfWork.Users.CountAsync(
                        u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                };

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Statistics retrieved successfully",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to retrieve statistics",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> GetUserDetailAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.Query()
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.Messages)
                    .Include(u => u.FriendshipUser1s)
                    .Include(u => u.FriendshipUser2s)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        Data = ""
                    };
                }
                var messageCount = await _unitOfWork.Messages.CountAsync(m => m.SenderId == userId);
                var friendCount = await _unitOfWork.Friendships.CountAsync(f => f.User1Id == userId || f.User2Id == userId);

                var userDetail = new AdminUserDetailDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Fullname = user.Fullname,
                    Avatar = user.Avatar,
                    Bio = user.Bio,
                    DateOfBirth = user.DateOfBirth,
                    IsActive = user.IsActive ?? true,
                    IsEmailVerified = user.IsEmailVerified ?? false,
                    CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = user.UpdatedAt,
                    LastLoginAt = user.LastLoginAt,
                    EmailVerifiedAt = user.EmailVerifiedAt,
                    FailedLoginAttempts = user.FailedLoginAttempts ?? 0,
                    AccountLockedUntil = user.AccountLockedUntil,
                    Roles = user.UserRoles.Select(ur => new RoleDto
                    {
                        RoleId = ur.RoleId,
                        RoleName = ur.Role.RoleName,
                        Description = ur.Role.Description,
                        AssignedAt = ur.AssignedAt
                    }).ToList(),
                    MessageCount = messageCount,
                    FriendCount = friendCount
                };

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "User detail retrieved successfully",
                    Data = userDetail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user detail for userId: {UserId}", userId);
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to retrieve user detail",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> GetUserRolesAsync(Guid userId)
        {
            try
            {
                var userRoles = await _unitOfWork.UserRoles.Query()
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Role)
                    .Select(ur => new UserRoleDto
                    {
                        UserRoleId = ur.UserRoleId,
                        UserId = ur.UserId,
                        RoleId = ur.RoleId,
                        RoleName = ur.Role.RoleName,
                        AssignedAt = ur.AssignedAt.GetValueOrDefault()
                    })
                    .ToListAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "User roles retrieved successfully",
                    Data = userRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to retrieve user roles",
                    Data = ""
                };
            }
        }



        public async Task<ApiResponseDto> RemoveRoleAsync(Guid userId, int roleId)
        {
            try
            {
                var userRole = await _unitOfWork.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "Role assignment not found",
                        Data = ""
                    };
                }

                _unitOfWork.UserRoles.Remove(userRole);
                await _unitOfWork.SaveChangesAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Role removed successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role");
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to remove role",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> ResetUserPasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        Data = ""
                    };
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.PasswordChangedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Password reset successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to reset password",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> ToggleUserStatusAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        Data = ""
                    };
                }

                user.IsActive = !(user.IsActive ?? true);
                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                var status = user.IsActive.Value ? "activated" : "deactivated";
                return new ApiResponseDto
                {
                    Success = true,
                    Message = $"User {status} successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status {UserId}", userId);
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to toggle user status",
                    Data = ""
                };
            }
        }

        public async Task<ApiResponseDto> UpdateUserAsync(Guid userId, UpdateUserByAdminDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto
                    {
                        Success = false,
                        Message = "User not found",
                        Data = ""
                    };
                }

                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    var existingEmail = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                    if (existingEmail != null)
                    {
                        return new ApiResponseDto
                        {
                            Success = false,
                            Message = "Email already exists",
                            Data = ""
                        };
                    }
                    user.Email = dto.Email;
                }

                if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
                {
                    var existingUsername = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
                    if (existingUsername != null)
                    {
                        return new ApiResponseDto
                        {
                            Success = false,
                            Message = "Username already exists",
                            Data = ""
                        };
                    }
                    user.Username = dto.Username;
                }

                user.Fullname = dto.Fullname ?? user.Fullname;
                user.Bio = dto.Bio ?? user.Bio;
                user.DateOfBirth = dto.DateOfBirth ?? user.DateOfBirth;

                if (dto.IsActive.HasValue)
                    user.IsActive = dto.IsActive.Value;

                if (dto.IsEmailVerified.HasValue)
                    user.IsEmailVerified = dto.IsEmailVerified.Value;

                user.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                var roles = await _unitOfWork.UserRoles
                    .Query()
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role.RoleName)
                    .ToListAsync();

                var userDto = new AdminUserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Fullname = user.Fullname,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive ?? true,
                    IsEmailVerified = user.IsEmailVerified ?? false,
                    CreatedAt = user.CreatedAt ?? DateTime.UtcNow,
                    LastLoginAt = user.LastLoginAt,
                    Roles = roles
                };

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "User updated successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "Failed to update user",
                    Data = ""
                };
            }
        }
        // ─────────────────────────────────────────────────────────────────────────────
        // Drop this method into AdminService in place of the current stub.
        //
        // Strategy
        // ─────────────────────────────────────────────────────────────────────────────
        // DeleteUserAsync already contains the full cascade logic but refuses to delete
        // the last admin.  HardDeleteUserAsync intentionally bypasses that guard — it is
        // meant for situations where an admin needs to be force-removed (e.g. test data
        // cleanup, GDPR erasure).  Everything else is identical.
        // ─────────────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> HardDeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user is null)
                    return new ApiResponseDto { Success = false, Message = "User not found", Data = "" };

                await _unitOfWork.BeginTransactionAsync();

                // 1. Access tokens
                var tokens = await _unitOfWork.AccessTokens.Query()
                    .Where(t => t.UserId == userId).ToListAsync();
                _unitOfWork.AccessTokens.RemoveRange(tokens);

                // 2. Email OTPs
                var otps = await _unitOfWork.EmailOtps.Query()
                    .Where(o => o.UserId == userId).ToListAsync();
                _unitOfWork.EmailOtps.RemoveRange(otps);

                // 3. Friend requests (both directions)
                var friendRequests = await _unitOfWork.FriendRequests.Query()
                    .Where(fr => fr.SenderId == userId || fr.ReceiverId == userId).ToListAsync();
                _unitOfWork.FriendRequests.RemoveRange(friendRequests);

                // 4. Friendships
                var friendships = await _unitOfWork.Friendships.Query()
                    .Where(f => f.User1Id == userId || f.User2Id == userId).ToListAsync();
                _unitOfWork.Friendships.RemoveRange(friendships);

                // 5. Blocks (both directions)
                var blocks = await _unitOfWork.BlockedUsers.Query()
                    .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId).ToListAsync();
                _unitOfWork.BlockedUsers.RemoveRange(blocks);

                // 6. Notifications
                var notifications = await _unitOfWork.Notifications.Query()
                    .Where(n => n.UserId == userId).ToListAsync();
                _unitOfWork.Notifications.RemoveRange(notifications);

                // 7. User roles
                var userRoles = await _unitOfWork.UserRoles.Query()
                    .Where(ur => ur.UserId == userId).ToListAsync();
                _unitOfWork.UserRoles.RemoveRange(userRoles);

                // 8. Group invitations
                var invitations = await _unitOfWork.GroupInvitations.Query()
                    .Where(gi => gi.InvitedUserId == userId || gi.InvitedBy == userId).ToListAsync();
                _unitOfWork.GroupInvitations.RemoveRange(invitations);

                // 9. Chatroom memberships
                var memberships = await _unitOfWork.ChatroomMembers.Query()
                    .Where(cm => cm.UserId == userId).ToListAsync();
                _unitOfWork.ChatroomMembers.RemoveRange(memberships);

                // 10. Nullify last_message_id on chatrooms that reference this user's messages
                //     (must happen BEFORE deleting the messages themselves)
                var userMessageIds = await _unitOfWork.Messages.Query()
                    .Where(m => m.SenderId == userId)
                    .Select(m => m.MessageId)
                    .ToListAsync();

                if (userMessageIds.Any())
                {
                    var affectedChatrooms = await _unitOfWork.Chatrooms.Query()
                        .Where(c => c.LastMessageId.HasValue &&
                                    userMessageIds.Contains(c.LastMessageId.Value))
                        .ToListAsync();

                    foreach (var chatroom in affectedChatrooms)
                        chatroom.LastMessageId = null;

                    _unitOfWork.Chatrooms.UpdateRange(affectedChatrooms);
                }

                // 11. Handle chatrooms created by this user
                var ownedChatrooms = await _unitOfWork.Chatrooms.Query()
                    .Where(c => c.CreatedBy == userId).ToListAsync();

                foreach (var chatroom in ownedChatrooms.Where(c => c.LastMessageId.HasValue))
                    chatroom.LastMessageId = null;

                _unitOfWork.Chatrooms.UpdateRange(ownedChatrooms);
                await _unitOfWork.SaveChangesAsync(); // flush NULL updates first

                // 12. Delete the user's messages
                var userMessages = await _unitOfWork.Messages.Query()
                    .Where(m => m.SenderId == userId).ToListAsync();
                _unitOfWork.Messages.RemoveRange(userMessages);

                // 13. For each owned chatroom: transfer ownership or delete
                foreach (var chatroom in ownedChatrooms)
                {
                    if (chatroom.RoomType == "direct")
                    {
                        var chatroomMessages = await _unitOfWork.Messages.Query()
                            .Where(m => m.ChatroomId == chatroom.ChatroomId).ToListAsync();
                        _unitOfWork.Messages.RemoveRange(chatroomMessages);

                        var chatroomMembers = await _unitOfWork.ChatroomMembers.Query()
                            .Where(cm => cm.ChatroomId == chatroom.ChatroomId).ToListAsync();
                        _unitOfWork.ChatroomMembers.RemoveRange(chatroomMembers);

                        _unitOfWork.Chatrooms.Remove(chatroom);
                    }
                    else
                    {
                        // Try to hand off to another active admin first, then any member
                        var newOwner = await _unitOfWork.ChatroomMembers.Query()
                            .Where(cm => cm.ChatroomId == chatroom.ChatroomId &&
                                         cm.UserId != userId &&
                                         cm.LeftAt == null &&
                                         cm.MemberRole == "admin")
                            .FirstOrDefaultAsync()
                            ??
                            await _unitOfWork.ChatroomMembers.Query()
                            .Where(cm => cm.ChatroomId == chatroom.ChatroomId &&
                                         cm.UserId != userId &&
                                         cm.LeftAt == null)
                            .OrderBy(cm => cm.JoinedAt)
                            .FirstOrDefaultAsync();

                        if (newOwner is not null)
                        {
                            chatroom.CreatedBy = newOwner.UserId;
                            newOwner.MemberRole = "admin";
                            _unitOfWork.Chatrooms.Update(chatroom);
                            _unitOfWork.ChatroomMembers.Update(newOwner);
                        }
                        else
                        {
                            // No remaining members — dissolve the room entirely
                            var chatroomMessages = await _unitOfWork.Messages.Query()
                                .Where(m => m.ChatroomId == chatroom.ChatroomId).ToListAsync();
                            _unitOfWork.Messages.RemoveRange(chatroomMessages);

                            var chatroomMembers = await _unitOfWork.ChatroomMembers.Query()
                                .Where(cm => cm.ChatroomId == chatroom.ChatroomId).ToListAsync();
                            _unitOfWork.ChatroomMembers.RemoveRange(chatroomMembers);

                            _unitOfWork.Chatrooms.Remove(chatroom);
                        }
                    }
                }

                // 14. Finally remove the user record itself
                _unitOfWork.Users.Remove(user);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("User {UserId} was hard-deleted by an administrator", userId);

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "User permanently and forcefully deleted",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error hard-deleting user {UserId}", userId);
                return new ApiResponseDto
                {
                    Success = false,
                    Message = $"Hard delete failed: {ex.Message}",
                    Data = ""
                };
            }
        }
    }
}