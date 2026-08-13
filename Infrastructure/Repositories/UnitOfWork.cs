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

        public UnitOfWork(LinksyDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
            InitializeRepositories();
        }

        private void InitializeRepositories()
        {
            // ?? Generic ??????????????????????????????????????????????????????
            Users = new Repository<User>(_context);
            Roles = new Repository<Role>(_context);
            UserRoles = new Repository<UserRole>(_context);
            AccessTokens = new Repository<AccessToken>(_context);
            EmailOtps = new Repository<EmailOtp>(_context);

            Chatrooms = new Repository<Chatroom>(_context);
            ChatroomMembers = new Repository<ChatroomMember>(_context);
            MemberPermissions = new Repository<MemberPermission>(_context);
            GroupInvitations = new Repository<GroupInvitation>(_context);

            Messages = new Repository<Message>(_context);
            MessageAttachments = new Repository<MessageAttachment>(_context);
            MessageReactions = new Repository<MessageReaction>(_context);
            MessageDeliveries = new Repository<MessageDelivery>(_context);
            MessageMentions = new Repository<MessageMention>(_context);
            PinnedMessages = new Repository<PinnedMessage>(_context);
            MessagePolls = new Repository<MessagePoll>(_context);
            MessagePollOptions = new Repository<MessagePollOption>(_context);
            MessagePollVotes = new Repository<MessagePollVote>(_context);

            CallLogs = new Repository<CallLog>(_context);
            CallParticipants = new Repository<CallParticipant>(_context);

            Friendships = new Repository<Friendship>(_context);
            FriendRequests = new Repository<FriendRequest>(_context);
            BlockedUsers = new Repository<BlockedUser>(_context);
            UserReports = new Repository<UserReport>(_context);

            Notifications = new Repository<Notification>(_context);

            UserSettingsRepo = new Repository<UserSettings>(_context);
            NotificationSettingsRepo = new Repository<NotificationSettings>(_context);
            PrivacySettingsRepo = new Repository<PrivacySettings>(_context);
            UserStatusRepo = new Repository<UserStatus>(_context);

            ChatroomRepository = new ChatroomRepository(_context);
            ChatroomMemberRepository = new ChatroomMemberRepository(_context);
            GroupInvitationRepository = new GroupInvitationRepository(_context);
            UserRepository = new UserRepository(_context);
            MessageRepository = new MessageRepository(_context);
            MemberPermissionRepository = new MemberPermissionRepository(_context);
            FriendRequestRepository = new FriendRequestRepository(_context);
            FriendshipRepository = new FriendshipRepository(_context);
            BlockedUserRepository = new BlockedUserRepository(_context);
            NotificationRepository = new NotificationRepository(_context);
            MessageReactionRepository = new MessageReactionRepository(_context);
            MessageDeliveryRepository = new MessageDeliveryRepository(_context);
            MessageAttachmentRepository = new MessageAttachmentRepository(_context);
            UserSettingsRepository = new UserSettingsRepository(_context);
            NotificationSettingsRepository = new NotificationSettingsRepository(_context);
            PrivacySettingsRepository = new PrivacySettingsRepository(_context);
            UserStatusRepository = new UserStatusRepository(_context);
            TokenRepository = new TokenRepository(_context);
        }

        #region Generic Repositories
        public IRepository<User> Users { get; private set; } = null!;
        public IRepository<Role> Roles { get; private set; } = null!;
        public IRepository<UserRole> UserRoles { get; private set; } = null!;
        public IRepository<AccessToken> AccessTokens { get; private set; } = null!;
        public IRepository<EmailOtp> EmailOtps { get; private set; } = null!;
        public IRepository<Chatroom> Chatrooms { get; private set; } = null!;
        public IRepository<ChatroomMember> ChatroomMembers { get; private set; } = null!;
        public IRepository<MemberPermission> MemberPermissions { get; private set; } = null!;
        public IRepository<GroupInvitation> GroupInvitations { get; private set; } = null!;
        public IRepository<Message> Messages { get; private set; } = null!;
        public IRepository<MessageAttachment> MessageAttachments { get; private set; } = null!;
        public IRepository<MessageReaction> MessageReactions { get; private set; } = null!;
        public IRepository<MessageDelivery> MessageDeliveries { get; private set; } = null!;
        public IRepository<MessageMention> MessageMentions { get; private set; } = null!;
        public IRepository<PinnedMessage> PinnedMessages { get; private set; } = null!;
        public IRepository<MessagePoll> MessagePolls { get; private set; } = null!;
        public IRepository<MessagePollOption> MessagePollOptions { get; private set; } = null!;
        public IRepository<MessagePollVote> MessagePollVotes { get; private set; } = null!;
        public IRepository<CallLog> CallLogs { get; private set; } = null!;
        public IRepository<CallParticipant> CallParticipants { get; private set; } = null!;
        public IRepository<Friendship> Friendships { get; private set; } = null!;
        public IRepository<FriendRequest> FriendRequests { get; private set; } = null!;
        public IRepository<BlockedUser> BlockedUsers { get; private set; } = null!;
        public IRepository<UserReport> UserReports { get; private set; } = null!;
        public IRepository<Notification> Notifications { get; private set; } = null!;
        public IRepository<UserSettings> UserSettingsRepo { get; private set; } = null!;
        public IRepository<NotificationSettings> NotificationSettingsRepo { get; private set; } = null!;
        public IRepository<PrivacySettings> PrivacySettingsRepo { get; private set; } = null!;
        public IRepository<UserStatus> UserStatusRepo { get; private set; } = null!;
        #endregion

        #region Specialized Repositories
        public IChatroomRepository ChatroomRepository { get; private set; } = null!;
        public IChatroomMemberRepository ChatroomMemberRepository { get; private set; } = null!;
        public IGroupInvitationRepository GroupInvitationRepository { get; private set; } = null!;
        public IUserRepository UserRepository { get; private set; } = null!;
        public IMessageRepository MessageRepository { get; private set; } = null!;
        public IMemberPermissionRepository MemberPermissionRepository { get; private set; } = null!;
        public IFriendRequestRepository FriendRequestRepository { get; private set; } = null!;
        public IFriendshipRepository FriendshipRepository { get; private set; } = null!;
        public IBlockedUserRepository BlockedUserRepository { get; private set; } = null!;
        public INotificationRepository NotificationRepository { get; private set; } = null!;
        public IMessageReactionRepository MessageReactionRepository { get; private set; } = null!;
        public IMessageDeliveryRepository MessageDeliveryRepository { get; private set; } = null!;
        public IMessageAttachmentRepository MessageAttachmentRepository { get; private set; } = null!;
        public IUserSettingsRepository UserSettingsRepository { get; private set; } = null!;
        public INotificationSettingsRepository NotificationSettingsRepository { get; private set; } = null!;
        public IPrivacySettingsRepository PrivacySettingsRepository { get; private set; } = null!;
        public IUserStatusRepository UserStatusRepository { get; private set; } = null!;
        public ITokenRepository TokenRepository { get; private set; } = null!;
        #endregion

        #region Transaction Management
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null) return;
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null) return;
            try
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
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
            if (_transaction == null) return;
            try { await _transaction.RollbackAsync(); }
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
            try { return await _context.SaveChangesAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error saving changes"); throw; }
        }

        public int SaveChanges()
        {
            try { return _context.SaveChanges(); }
            catch (Exception ex) { _logger.LogError(ex, "Error saving changes"); throw; }
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
                }
                _disposed = true;
            }
        }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        #endregion
    }
}
