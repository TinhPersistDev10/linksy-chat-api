using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class StickerConfiguration : IEntityTypeConfiguration<Sticker>
    {
        public void Configure(EntityTypeBuilder<Sticker> entity)
        {
            entity.HasKey(e => e.Id).HasName("stickers_pkey");
            entity.ToTable("stickers");

            entity.HasIndex(e => e.OwnerId)
                .HasDatabaseName("stickers_owner_id_idx");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.OwnerId)
                .HasColumnName("owner_id");
            entity.Property(e => e.ImageUrl)
                .HasColumnName("image_url");
            entity.Property(e => e.PublicId)
                .HasColumnName("public_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Owner)
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("stickers_owner_id_fkey");
        }
    }
}
