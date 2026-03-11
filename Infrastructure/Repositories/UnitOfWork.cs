using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Repositories;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Infrastructure.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace linksy_backend_api.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LinksyDbContext _context;
        private ILogger<UnitOfWork>? _logger;
        private IDbContextTransaction _transaction;
        
        private bool _disposed = false;

        #region Constructor
        public UnitOfWork(LinksyDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
            InitializeRepositories();
        }

        private void InitializeRepositories()
        {
            // User Management
            Users = new Repository<User>(_context);
            Roles = new Repository<Role>(_context);
            UserRoles = new Repository<UserRole>(_context);
            AccessTokens = new Repository<AccessToken>(_context);
            EmailOtps = new Repository<EmailOtp>(_context);

            // Chatrooms
            Chatrooms = new Repository<Chatroom>(_context);
            ChatroomMembers = new Repository<ChatroomMember>(_context);
            MemberPermissions = new Repository<MemberPermission>(_context);
            GroupInvitations = new Repository<GroupInvitation>(_context);

            // Messages
            Messages = new Repository<Message>(_context);

            // Social
            Friendships = new Repository<Friendship>(_context);
            FriendRequests = new Repository<FriendRequest>(_context);
            BlockedUsers = new Repository<BlockedUser>(_context);

            // Notifications
            Notifications = new Repository<Notification>(_context);

            // Specialized Repositories
            ChatroomRepository = new ChatroomRepository(_context);
            UserRepository = new UserRepository(_context);
            MessageRepository = new MessageRepository(_context);
            MemberPermissionRepository = new MemberPermissionRepository(_context);
            FriendRequestRepository = new FriendRequestRepository(_context);
            FriendshipRepository  = new FriendshipRepository(_context);
            BlockedUserRepository = new BlockedUserRepository(_context);
            NotificationRepository = new NotificationRepository(_context);

            _logger.LogDebug("UnitOfWork repositories initialized");
        }
        #endregion
       
        #region Generic Repositories

        public IRepository<User> Users { get; private set; }
        public IRepository<Role> Roles { get; private set; }
        public IRepository<UserRole> UserRoles { get; private set; }
        public IRepository<AccessToken> AccessTokens { get; private set; }
        public IRepository<EmailOtp> EmailOtps { get; private set; }

        public IRepository<Chatroom> Chatrooms { get; private set; }
        public IRepository<ChatroomMember> ChatroomMembers { get; private set; }
        public IRepository<MemberPermission> MemberPermissions { get; private set; }
        public IRepository<GroupInvitation> GroupInvitations { get; private set; }

        public IRepository<Message> Messages { get; private set; }

        public IRepository<Friendship> Friendships { get; private set; }
        public IRepository<FriendRequest> FriendRequests { get; private set; }
        public IRepository<BlockedUser> BlockedUsers { get; private set; }

        public IRepository<Notification> Notifications { get; private set; }

        #endregion

        #region Specialized Repositories

        public IChatroomRepository ChatroomRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public IMessageRepository MessageRepository { get; private set; }
        public IMemberPermissionRepository MemberPermissionRepository { get; private set; }
        public IFriendRequestRepository FriendRequestRepository { get; private set; }
        public IFriendshipRepository FriendshipRepository { get; private set; }
        public IBlockedUserRepository BlockedUserRepository { get; private set; }
        public INotificationRepository NotificationRepository { get; private set; }
        #endregion

        #region Transaction Management

        /// Bắt đầu một database transaction mới
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogWarning("Transaction already exists. Skipping BeginTransactionAsync.");
                return;
            }

            try
            {
                _transaction = await _context.Database.BeginTransactionAsync();
                _logger.LogDebug("Database transaction started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting database transaction");
                throw;
            }
        }

        /// Commit transaction và lưu tất cả thay đổi
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to commit");
                return;
            }

            try
            {
                // Save changes first
                await SaveChangesAsync();

                // Then commit transaction
                await _transaction.CommitAsync();
                _logger.LogDebug("Database transaction committed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing database transaction");
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        /// Rollback transaction và hủy tất cả thay đổi
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to rollback");
                return;
            }

            try
            {
                await _transaction.RollbackAsync();
                _logger.LogDebug("Database transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back database transaction");
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        #endregion

        #region Save Changes

        /// Lưu tất cả thay đổi vào database (async)
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                var result = await _context.SaveChangesAsync();
                _logger.LogDebug("Saved {Count} changes to database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        /// Lưu tất cả thay đổi vào database (sync)
        public int SaveChanges()
        {
            try
            {
                var result = _context.SaveChanges();
                _logger.LogDebug("Saved {Count} changes to database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        #endregion

        #region Dispose

        /// Dispose resources
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _transaction?.Dispose();
                    _context?.Dispose();
                    _logger.LogDebug("UnitOfWork disposed");
                }

                _disposed = true;
            }
        }

        /// Public dispose method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}