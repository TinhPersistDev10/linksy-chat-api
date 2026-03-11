using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
    {
        public void Configure(EntityTypeBuilder<BlockedUser> entity)
        {
            entity.HasKey(e => e.BlockId).HasName("blocked_users_pkey");

            entity.ToTable("blocked_users", tb => tb.HasComment("CHECK (blocker_user_id != blocked_user_id)"));

            entity.HasIndex(e => new { e.BlockedUserId, e.BlockerUserId }, "blocked_users_blocked_user_id_blocker_user_id_idx");
            entity.HasIndex(e => new { e.BlockerUserId, e.BlockedAt }, "blocked_users_blocker_user_id_blocked_at_idx");
            entity.HasIndex(e => new { e.BlockerUserId, e.BlockedUserId }, "blocked_users_blocker_user_id_blocked_user_id_idx").IsUnique();

            entity.Property(e => e.BlockId)
                .ValueGeneratedNever()
                .HasColumnName("block_id");
            entity.Property(e => e.BlockedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("blocked_at");
            entity.Property(e => e.BlockedUserId).HasColumnName("blocked_user_id");
            entity.Property(e => e.BlockerUserId).HasColumnName("blocker_user_id");
            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.HasOne(d => d.BlockedUserNavigation).WithMany(p => p.BlockedUserBlockedUserNavigations)
                .HasForeignKey(d => d.BlockedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("blocked_users_blocked_user_id_fkey");

            entity.HasOne(d => d.BlockerUser).WithMany(p => p.BlockedUserBlockerUsers)
                .HasForeignKey(d => d.BlockerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("blocked_users_blocker_user_id_fkey");
        }
    }
}