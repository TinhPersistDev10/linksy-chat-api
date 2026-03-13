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
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _transaction;
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
            Users          = new Repository<User>(_context);
            Roles          = new Repository<Role>(_context);
            UserRoles      = new Repository<UserRole>(_context);
            AccessTokens   = new Repository<AccessToken>(_context);
            EmailOtps      = new Repository<EmailOtp>(_context);

            // Chatrooms
            Chatrooms         = new Repository<Chatroom>(_context);
            ChatroomMembers   = new Repository<ChatroomMember>(_context);
            MemberPermissions = new Repository<MemberPermission>(_context);
            GroupInvitations  = new Repository<GroupInvitation>(_context);

            // Messages
            Messages = new Repository<Message>(_context);

            // Social
            Friendships   = new Repository<Friendship>(_context);
            FriendRequests = new Repository<FriendRequest>(_context);
            BlockedUsers  = new Repository<BlockedUser>(_context);

            // Notifications
            Notifications = new Repository<Notification>(_context);

            // Specialized Repositories
            ChatroomRepository          = new ChatroomRepository(_context);
            ChatroomMemberRepository    = new ChatroomMemberRepository(_context);
            GroupInvitationRepository   = new GroupInvitationRepository(_context);
            UserRepository              = new UserRepository(_context);
            MessageRepository           = new MessageRepository(_context);
            MemberPermissionRepository  = new MemberPermissionRepository(_context);
            FriendRequestRepository     = new FriendRequestRepository(_context);
            FriendshipRepository        = new FriendshipRepository(_context);
            BlockedUserRepository       = new BlockedUserRepository(_context);
            NotificationRepository      = new NotificationRepository(_context);
            
            _logger.LogDebug("UnitOfWork repositories initialized");
        }

        #endregion

        #region Generic Repositories

        // User Management
        public IRepository<User>        Users        { get; private set; } = null!;
        public IRepository<Role>        Roles        { get; private set; }= null!;
        public IRepository<UserRole>    UserRoles    { get; private set; }= null!;
        public IRepository<AccessToken> AccessTokens { get; private set; }= null!;
        public IRepository<EmailOtp>    EmailOtps    { get; private set; }= null!;

        // Chatrooms
        public IRepository<Chatroom>       Chatrooms         { get; private set; }= null!;
        public IRepository<ChatroomMember> ChatroomMembers   { get; private set; }= null!;
        public IRepository<MemberPermission> MemberPermissions { get; private set; }= null!;
        public IRepository<GroupInvitation>  GroupInvitations  { get; private set; }= null!;

        // Messages
        public IRepository<Message> Messages { get; private set; }= null!;

        // Social
        public IRepository<Friendship>   Friendships    { get; private set; }= null!;
        public IRepository<FriendRequest> FriendRequests { get; private set; }= null!;
        public IRepository<BlockedUser>  BlockedUsers   { get; private set; }= null!;

        // Notifications
        public IRepository<Notification> Notifications { get; private set; }= null!;

        #endregion

        #region Specialized Repositories

        public IChatroomRepository         ChatroomRepository         { get; private set; }= null!;
        public IChatroomMemberRepository   ChatroomMemberRepository   { get; private set; }= null!;
        public IGroupInvitationRepository  GroupInvitationRepository  { get; private set; }= null!;
        public IUserRepository             UserRepository             { get; private set; }= null!;
        public IMessageRepository          MessageRepository          { get; private set; }= null!;
        public IMemberPermissionRepository MemberPermissionRepository { get; private set; }= null!;
        public IFriendRequestRepository    FriendRequestRepository    { get; private set; }= null!;
        public IFriendshipRepository       FriendshipRepository       { get; private set; }= null!;
        public IBlockedUserRepository      BlockedUserRepository      { get; private set; }= null!;
        public INotificationRepository     NotificationRepository     { get; private set; }= null!;

        #endregion

        #region Transaction Management

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

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No active transaction to commit");
                return;
            }

            try
            {
                await SaveChangesAsync();
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
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

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
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        #endregion

        #region Save Changes

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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                    _logger.LogDebug("UnitOfWork disposed");
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}