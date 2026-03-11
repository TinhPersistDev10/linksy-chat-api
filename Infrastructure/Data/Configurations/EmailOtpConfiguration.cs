using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
    {
        public void Configure(EntityTypeBuilder<EmailOtp> entity)
        {
            entity.HasKey(e => e.Id).HasName("email_otps_pkey");

            entity.ToTable("email_otps");

            entity.HasIndex(e => e.CreatedAt, "email_otps_created_at_idx");
            entity.HasIndex(e => new { e.Email, e.OtpCode, e.IsUsed, e.IsExpired }, "email_otps_email_otp_code_is_used_is_expired_idx");
            entity.HasIndex(e => e.ExpiresAt, "email_otps_expires_at_idx");
            entity.HasIndex(e => new { e.UserId, e.Purpose }, "email_otps_user_id_purpose_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Attempts)
                .HasDefaultValue(0)
                .HasColumnName("attempts");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            entity.Property(e => e.IsExpired)
                .HasDefaultValue(false)
                .HasColumnName("is_expired");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.MaxAttempts)
                .HasDefaultValue(5)
                .HasColumnName("max_attempts");
            entity.Property(e => e.OtpCode)
                .HasColumnType("character varying")
                .HasColumnName("otp_code");
            entity.Property(e => e.Purpose)
                .HasColumnType("character varying")
                .HasColumnName("purpose");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerifiedAt)
                .HasColumnName("verified_at");

            entity.HasOne(d => d.User).WithMany(p => p.EmailOtps)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("email_otps_user_id_fkey");

        }
    }
}