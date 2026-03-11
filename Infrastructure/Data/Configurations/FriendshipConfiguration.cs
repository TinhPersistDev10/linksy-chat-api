using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> entity)
        {
            entity.HasKey(e => e.FriendshipId).HasName("friendships_pkey");
            entity.ToTable("friendships");

            entity.Property(e => e.FriendshipId).HasColumnName("friendship_id");
            entity.Property(e => e.User1Id).HasColumnName("user1_id");
            entity.Property(e => e.User2Id).HasColumnName("user2_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at")
                .HasDefaultValueSql("now()");

            // Indexes - SỬA LẠI
            entity.HasIndex(e => new { e.User1Id, e.User2Id })
                .IsUnique()
                .HasDatabaseName("friendships_user1_id_user2_id_key");

            entity.HasIndex(e => e.User1Id)
                .HasDatabaseName("friendships_user1_id_idx");

            entity.HasIndex(e => e.User2Id)
                .HasDatabaseName("friendships_user2_id_idx");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("friendships_created_at_idx");

            // Relationships - SỬA LẠI cho đúng
            entity.HasOne(d => d.User1)
                .WithMany(u => u.FriendshipUser1s) // Khớp với navigation property trong User
                .HasForeignKey(d => d.User1Id)
                .OnDelete(DeleteBehavior.Cascade) // Dùng Cascade như thiết kế
                .HasConstraintName("friendships_user1_id_fkey");

            entity.HasOne(d => d.User2)
                .WithMany(u => u.FriendshipUser2s) // Khớp với navigation property trong User
                .HasForeignKey(d => d.User2Id)
                .OnDelete(DeleteBehavior.Cascade) // Dùng Cascade như thiết kế
                .HasConstraintName("friendships_user2_id_fkey");

            // Thêm Check Constraint
            entity.HasCheckConstraint("CK_Friendships_DifferentUsers",
                "user1_id != user2_id");
        }
    }
}