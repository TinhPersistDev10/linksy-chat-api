using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
    {
        public void Configure(EntityTypeBuilder<NotificationSettings> entity)
        {
            entity.HasKey(e => e.Id).HasName("notification_settings_pkey");

            entity.ToTable("notification_settings");

            entity.HasIndex(e => e.UserId, "notification_settings_user_id_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.NotificationsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("notifications_enabled");
            entity.Property(e => e.NotificationSoundEnabled)
                .HasDefaultValue(true)
                .HasColumnName("notification_sound_enabled");
            entity.Property(e => e.MessagePreviewEnabled)
                .HasDefaultValue(true)
                .HasColumnName("message_preview_enabled");
            entity.Property(e => e.EmailNotifications)
                .HasDefaultValue(false)
                .HasColumnName("email_notifications");

            entity.HasOne(d => d.User)
                .WithOne(p => p.NotificationSettings)
                .HasForeignKey<NotificationSettings>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notification_settings_user_id_fkey");
        }
    }
}