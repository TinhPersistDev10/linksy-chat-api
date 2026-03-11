    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using linksy_backend_api.Core.DTOs.Requests.Friends;
    using linksy_backend_api.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    namespace linksy_backend_api.Infrastructure.Data.Configurations
    {
        public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
        {
            public void Configure(EntityTypeBuilder<FriendRequest> entity)
            {
                entity.HasKey(e => e.RequestId).HasName("friend_requests_pkey");
                entity.ToTable("friend_requests");

                // Map properties to snake_case columns
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.SenderId).HasColumnName("sender_id");
                entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
                entity.Property(e => e.Status).HasColumnName("status")
                    .HasDefaultValueSql("'pending'::character varying")
                    .HasConversion<string>(); // Thêm conversion cho an toàn
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.SentAt).HasColumnName("sent_at")
                    .HasDefaultValueSql("now()");
                entity.Property(e => e.RespondedAt).HasColumnName("responded_at");

                // Indexes - SỬA LẠI CHO ĐÚNG
                entity.HasIndex(e => new { e.ReceiverId, e.Status })
                    .HasDatabaseName("friend_requests_receiver_id_status_idx");

                entity.HasIndex(e => new { e.SenderId, e.Status })
                    .HasDatabaseName("friend_requests_sender_id_status_idx");

                entity.HasIndex(e => e.SentAt)
                    .HasDatabaseName("friend_requests_sent_at_idx");

                // QUAN TRỌNG: Sửa relationships - phải khớp với tên navigation properties trong User model
                entity.HasOne(d => d.Receiver)
                    .WithMany(p => p.ReceivedFriendRequests) // Phải khớp với tên trong User
                    .HasForeignKey(d => d.ReceiverId)
                    .OnDelete(DeleteBehavior.Cascade) // Nên dùng Cascade như thiết kế ban đầu
                    .HasConstraintName("friend_requests_receiver_id_fkey");

                entity.HasOne(d => d.Sender)
                    .WithMany(p => p.SentFriendRequests) // Phải khớp với tên trong User
                    .HasForeignKey(d => d.SenderId)
                    .OnDelete(DeleteBehavior.Cascade) // Nên dùng Cascade như thiết kế ban đầu
                    .HasConstraintName("friend_requests_sender_id_fkey");

                // Thêm Check Constraints
                entity.HasCheckConstraint("CK_FriendRequests_Status",
                    "status IN ('pending', 'accepted', 'rejected', 'cancelled')");

                entity.HasCheckConstraint("CK_FriendRequests_DifferentUsers",
                    "sender_id != receiver_id");
            }
        }
    }