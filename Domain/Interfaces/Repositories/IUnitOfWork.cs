using linksy_backend_api.Core.Interfaces.Repositories;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        #region User management repositories
        IRepository<User> Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<AccessToken> AccessTokens { get; }
        IRepository<EmailOtp> EmailOtps { get; }
        #endregion

        #region Chatroom repositories
        IRepository<Chatroom> Chatrooms { get; }
        IRepository<ChatroomMember> ChatroomMembers { get; }
        IRepository<GroupInvitation> GroupInvitations { get; }
        IRepository<MemberPermission> MemberPermissions { get; }
        #endregion

        #region Message repositories
        IRepository<Message> Messages { get; }
        IRepository<MessageAttachment> MessageAttachments { get; }
        IRepository<MessageReaction> MessageReactions { get; }
        IRepository<MessageDelivery> MessageDeliveries { get; }
        IRepository<MessageMention> MessageMentions { get; }
        IRepository<PinnedMessage> PinnedMessages { get; }
        IRepository<MessagePoll> MessagePolls { get; }
        IRepository<MessagePollOption> MessagePollOptions { get; }
        IRepository<MessagePollVote> MessagePollVotes { get; }
        #endregion

        #region Call repositories
        IRepository<CallLog> CallLogs { get; }
        IRepository<CallParticipant> CallParticipants { get; }
        #endregion

        #region Social repositories
        IRepository<Friendship> Friendships { get; }
        IRepository<FriendRequest> FriendRequests { get; }
        IRepository<BlockedUser> BlockedUsers { get; }
        IRepository<UserReport> UserReports { get; }
        #endregion

        #region Notification repositories
        IRepository<Notification> Notifications { get; }
        #endregion

        #region Settings repositories
        IRepository<UserSettings> UserSettingsRepo { get; }
        IRepository<NotificationSettings> NotificationSettingsRepo { get; }
        IRepository<PrivacySettings> PrivacySettingsRepo { get; }
        IRepository<UserStatus> UserStatusRepo { get; }
        #endregion

        #region Specialized repositories
        IChatroomRepository ChatroomRepository { get; }
        IChatroomMemberRepository ChatroomMemberRepository { get; }
        IGroupInvitationRepository GroupInvitationRepository { get; }
        IUserRepository UserRepository { get; }
        IMessageRepository MessageRepository { get; }
        IMemberPermissionRepository MemberPermissionRepository { get; }
        IFriendRequestRepository FriendRequestRepository { get; }
        IFriendshipRepository FriendshipRepository { get; }
        IBlockedUserRepository BlockedUserRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IMessageReactionRepository MessageReactionRepository { get; }
        IMessageDeliveryRepository MessageDeliveryRepository { get; }
        IMessageAttachmentRepository MessageAttachmentRepository { get; }
        IUserSettingsRepository UserSettingsRepository { get; }
        INotificationSettingsRepository NotificationSettingsRepository { get; }
        IPrivacySettingsRepository PrivacySettingsRepository { get; }
        IUserStatusRepository UserStatusRepository { get; }
        ITokenRepository TokenRepository { get; }
        #endregion

        #region Transaction methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        int SaveChanges();
        #endregion
    }
}
