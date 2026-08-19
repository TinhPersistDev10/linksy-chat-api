using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class ContentModerationSettingConfiguration : IEntityTypeConfiguration<ContentModerationSetting>
    {
        public void Configure(EntityTypeBuilder<ContentModerationSetting> entity)
        {
            entity.HasKey(e => e.Id).HasName("content_moderation_settings_pkey");
            entity.ToTable("content_moderation_settings");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Enabled)
                .HasColumnName("enabled");
            entity.Property(e => e.BannedWords)
                .HasColumnType("text[]")
                .HasColumnName("banned_words");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId)
                .HasColumnName("updated_by_user_id");
        }
    }
}
