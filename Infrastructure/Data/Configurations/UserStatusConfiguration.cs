using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class UserStatusConfiguration : IEntityTypeConfiguration<UserStatus>
    {
        public void Configure(EntityTypeBuilder<UserStatus> entity)
        {
            entity.HasKey(e => e.StatusId).HasName("user_status_pkey");

            entity.ToTable("user_status");

            entity.HasIndex(e => e.UserId, "user_status_user_id_key").IsUnique();
            entity.HasIndex(e => new { e.UserId, e.StatusType }, "user_status_user_id_status_type_idx");

            entity.Property(e => e.StatusId)
                .ValueGeneratedNever()
                .HasColumnName("status_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.StatusType)
                .HasDefaultValueSql("'offline'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status_type");
            entity.Property(e => e.CustomStatus)
                .HasColumnType("character varying")
                .HasColumnName("custom_status");
            entity.Property(e => e.LastSeenAt)
                .HasColumnName("last_seen_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User)
                .WithOne(p => p.UserStatus)
                .HasForeignKey<UserStatus>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_status_user_id_fkey");
        }
    }
}