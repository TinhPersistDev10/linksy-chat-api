using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");
            entity.ToTable("users");

            // Indexes - OK
            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();
            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();
            entity.HasIndex(e => e.Email, "users_email_idx");
            entity.HasIndex(e => new { e.IsActive, e.CreatedAt }, "users_is_active_created_at_idx");
            entity.HasIndex(e => e.LastLoginAt, "users_last_login_at_idx");
            entity.HasIndex(e => e.Username, "users_username_idx");

            // Properties - ĐÃ OK
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.AccountLockedUntil)
                .HasColumnName("account_locked_until");
            entity.Property(e => e.Avatar)
                .HasColumnType("character varying")
                .HasColumnName("avatar");
            entity.Property(e => e.Bio)
                .HasColumnType("character varying")
                .HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.EmailVerifiedAt)
                .HasColumnName("email_verified_at");
            entity.Property(e => e.FailedLoginAttempts)
                .HasDefaultValue(0)
                .HasColumnName("failed_login_attempts");
            entity.Property(e => e.Fullname)
                .HasColumnType("character varying")
                .HasColumnName("fullname");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsEmailVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_email_verified");
            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");
            entity.Property(e => e.PasswordChangedAt)
                .HasColumnName("password_changed_at");
            entity.Property(e => e.PasswordHash)
                .HasColumnType("character varying")
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasColumnType("character varying")
                .HasColumnName("username");

            entity.Property(e => e.ModerationLevel)
                .HasMaxLength(30)
                .HasDefaultValue("none")
                .HasColumnName("moderation_level");
            entity.Property(e => e.ModerationReason)
                .HasMaxLength(500)
                .HasColumnName("moderation_reason");
            entity.Property(e => e.ModerationExpiresAt)
                .HasColumnName("moderation_expires_at");
            entity.Property(e => e.ModeratedAt)
                .HasColumnName("moderated_at");
            entity.Property(e => e.ModeratedByAdminId)
                .HasColumnName("moderated_by_admin_id");
            entity.Property(e => e.ViolationPoints)
                .HasDefaultValue(0)
                .HasColumnName("violation_points");
            entity.Property(e => e.IsFlaggedForReview)
                .HasDefaultValue(false)
                .HasColumnName("is_flagged_for_review");

            entity.HasIndex(e => e.ModerationLevel, "users_moderation_level_idx");
            entity.HasIndex(e => e.IsFlaggedForReview, "users_is_flagged_for_review_idx");

            entity.HasOne(d => d.ModeratedByAdmin)
                .WithMany()
                .HasForeignKey(d => d.ModeratedByAdminId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_moderated_by_admin_id_fkey");
        }


    }
}