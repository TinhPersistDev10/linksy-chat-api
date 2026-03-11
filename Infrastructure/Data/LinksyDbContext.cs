using System;
using System.Collections.Generic;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Models;

public partial class LinksyDbContext : DbContext
{
    public LinksyDbContext()
    {
    }

    public LinksyDbContext(DbContextOptions<LinksyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessToken> AccessTokens { get; set; }
    public virtual DbSet<BlockedUser> BlockedUsers { get; set; }
    public virtual DbSet<Chatroom> Chatrooms { get; set; }
    public virtual DbSet<EmailOtp> EmailOtps { get; set; }
    public virtual DbSet<FriendRequest> FriendRequests { get; set; }
    public virtual DbSet<Friendship> Friendships { get; set; }
    public virtual DbSet<GroupInvitation> GroupInvitations { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<MemberPermission> MemberPermissions { get; set; }
    public virtual DbSet<ChatroomMember> ChatroomMembers { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cấu hình tất cả DateTime properties thành timestamp with time zone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }

        // Áp dụng tất cả configurations
        modelBuilder.ApplyConfiguration(new AccessTokenConfiguration());
        modelBuilder.ApplyConfiguration(new BlockedUserConfiguration());
        modelBuilder.ApplyConfiguration(new ChatroomConfiguration());
        modelBuilder.ApplyConfiguration(new EmailOtpConfiguration());
        modelBuilder.ApplyConfiguration(new FriendRequestConfiguration());
        modelBuilder.ApplyConfiguration(new FriendshipConfiguration());
        modelBuilder.ApplyConfiguration(new GroupInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new ChatroomMemberConfiguration());
        modelBuilder.ApplyConfiguration(new MemberPermissionConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}