using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class AccessTokenConfiguration : IEntityTypeConfiguration<AccessToken>
    {
        public void Configure(EntityTypeBuilder<AccessToken> entity)
        {
            entity.HasKey(e => e.TokenId).HasName("access_tokens_pkey");

            entity.ToTable("access_tokens");

            entity.HasIndex(e => e.ExpiresAt, "access_tokens_expires_at_idx");
            entity.HasIndex(e => e.Token, "access_tokens_token_idx").IsUnique();
            entity.HasIndex(e => e.Token, "access_tokens_token_key").IsUnique();
            entity.HasIndex(e => e.UserId, "access_tokens_user_id_idx");

            entity.Property(e => e.TokenId).HasColumnName("token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");
            entity.Property(e => e.RefreshToken)
                .HasColumnType("text")
                .HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiresAt)
                .HasColumnName("refresh_token_expires_at");
            entity.Property(e => e.Token)
                .HasColumnType("text")
                .HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AccessTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("access_tokens_user_id_fkey");
        }
    }
}