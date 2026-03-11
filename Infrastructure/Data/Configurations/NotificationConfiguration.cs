using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> entity)
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.CreatedAt, "notifications_created_at_idx");
            entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId }, "notifications_related_entity_type_related_entity_id_idx");
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.IsDeleted, e.CreatedAt }, "notifications_user_id_is_read_is_deleted_created_at_idx");
            entity.HasIndex(e => new { e.UserId, e.NotificationType, e.CreatedAt }, "notifications_user_id_notification_type_created_at_idx");

            entity.Property(e => e.NotificationId)
                .ValueGeneratedNever()
                .HasColumnName("notification_id");
            entity.Property(e => e.ActionUrl)
                .HasColumnType("character varying")
                .HasColumnName("action_url");
            entity.Property(e => e.Body)
                .HasColumnType("character varying")
                .HasColumnName("body");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");
            entity.Property(e => e.ImageUrl)
                .HasColumnType("character varying")
                .HasColumnName("image_url");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.NotificationType)
                .HasColumnType("character varying")
                .HasColumnName("notification_type");
            entity.Property(e => e.ReadAt)
                .HasColumnName("read_at");
            entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
            entity.Property(e => e.RelatedEntityType)
                .HasColumnType("character varying")
                .HasColumnName("related_entity_type");
            entity.Property(e => e.Title)
                .HasColumnType("character varying")
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_user_id_fkey");
        }


    }
}