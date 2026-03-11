using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> entity)
        {
            entity.HasKey(e => e.SettingId).HasName("user_settings_pkey");

            entity.ToTable("user_settings");

            entity.HasIndex(e => e.UserId, "user_settings_user_id_key").IsUnique();

            entity.Property(e => e.SettingId)
                .ValueGeneratedNever()
                .HasColumnName("setting_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.Language)
                .HasDefaultValueSql("'vi'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("language");
            entity.Property(e => e.Timezone)
                .HasDefaultValueSql("'Asia/Ho_Chi_Minh'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("timezone");
            entity.Property(e => e.Theme)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("theme");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User)
                .WithOne(p => p.UserSettings)
                .HasForeignKey<UserSettings>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_settings_user_id_fkey");
        }
    }
}