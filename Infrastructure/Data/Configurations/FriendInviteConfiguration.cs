using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations;

public class FriendInviteConfiguration : IEntityTypeConfiguration<FriendInvite>
{
    public void Configure(EntityTypeBuilder<FriendInvite> entity)
    {
        entity.HasKey(e => e.Id).HasName("friend_invites_pkey");
        entity.ToTable("friend_invites");

        entity.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("friend_invites_token_uidx");

        entity.HasIndex(e => new { e.InviterId, e.IsUsed, e.ExpiresAt })
            .HasDatabaseName("friend_invites_inviter_active_idx");

        entity.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");
        entity.Property(e => e.Token)
            .HasMaxLength(128)
            .HasColumnName("token");
        entity.Property(e => e.InviterId).HasColumnName("inviter_id");
        entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        entity.Property(e => e.IsUsed)
            .HasDefaultValue(false)
            .HasColumnName("is_used");
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        entity.HasOne(d => d.Inviter)
            .WithMany()
            .HasForeignKey(d => d.InviterId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("friend_invites_inviter_id_fkey");
    }
}
