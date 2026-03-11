using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class PrivacySettingsConfiguration : IEntityTypeConfiguration<PrivacySettings>
    {
        public void Configure(EntityTypeBuilder<PrivacySettings> entity)
        {
            entity.HasKey(e => e.Id).HasName("privacy_settings_pkey");

            entity.ToTable("privacy_settings");

            entity.HasIndex(e => e.UserId, "privacy_settings_user_id_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.ReadReceiptsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("read_receipts_enabled");
            entity.Property(e => e.TypingIndicatorsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("typing_indicators_enabled");
            entity.Property(e => e.LastSeenEnabled)
                .HasDefaultValue(true)
                .HasColumnName("last_seen_enabled");
            entity.Property(e => e.ProfilePhotoVisibility)
                .HasDefaultValueSql("'everyone'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("profile_photo_visibility");
            entity.Property(e => e.StatusVisibility)
                .HasDefaultValueSql("'everyone'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status_visibility");
            entity.Property(e => e.WhoCanAddToGroups)
                .HasDefaultValueSql("'everyone'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("who_can_add_to_groups");

            entity.HasOne(d => d.User)
                .WithOne(p => p.PrivacySettings)
                .HasForeignKey<PrivacySettings>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("privacy_settings_user_id_fkey");
        }
    }
}